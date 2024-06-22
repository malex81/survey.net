using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace ImageProcessing.Engine;
public class EngineKernel : IDisposable
{
	private readonly ServiceProvider services;

	public EngineKernel()
	{
		AppConfig = BuildAppConfig();
		var serviceCollection = new ServiceCollection();
		//ConfigureLogging
		serviceCollection.AddSingleton<MainWindowModel>();

		services = serviceCollection.BuildServiceProvider();
	}

	public IServiceProvider Services => services;
	public IConfigurationRoot AppConfig { get; }

	static IConfigurationRoot BuildAppConfig()
	{
		var binPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
		var config = new ConfigurationBuilder();
		config.SetBasePath(Path.Combine(binPath!, "../config"))
					.AddJsonFile("appsettings.json", optional: false)
					.AddCommandLine(Environment.GetCommandLineArgs());
		return config.Build();
	}

	static ServiceCollection ConfigureLogging(ServiceCollection services, IConfigurationRoot appConfig)
	{
		var logConfigs = appConfig.GetSection("Logging");
		var serilogConfig = logConfigs.GetSection("Serilog");

		services.AddLogging(logging =>
		{
			logging.AddConfiguration(logConfigs);
			logging.AddConsole();
			logging.AddDebug();
			if (serilogConfig.GetValue<string>("PathFormat") != null)
				logging.AddFile(serilogConfig);
		});
		return services;
	}

	public void Dispose()
	{
		services.Dispose();
		GC.SuppressFinalize(this);
	}
}
