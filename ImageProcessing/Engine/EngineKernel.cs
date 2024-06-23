using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;

namespace ImageProcessing.Engine;
public class EngineKernel(ServiceProvider services, IConfigurationRoot config) : IDisposable
{
	public class Builder(IConfigurationRoot config)
	{
		private readonly IConfigurationRoot config = config;
		private readonly ServiceCollection serviceCollection = new();

		public Builder ConfigureServices(Action<IServiceCollection, IConfigurationRoot> configure)
		{
			//serviceCollection.AddSingleton<MainWindowModel>();
			//services = serviceCollection.BuildServiceProvider();
			configure(serviceCollection, config);
			return this;
		}

		public EngineKernel Build() => new(serviceCollection.BuildServiceProvider(), config);
	}

	public static Builder Configure(Action<ConfigurationBuilder> configure)
	{
		ConfigurationBuilder configBuilder = new();
		configure(configBuilder);
		return new(configBuilder.Build());
	}

	private readonly ServiceProvider services = services;

	public IServiceProvider Services => services;
	public IConfigurationRoot AppConfig { get; } = config;

	public void Dispose()
	{
		services.Dispose();
		GC.SuppressFinalize(this);
	}
}

public static class EngineKernelBuilder
{
	

	//static IConfigurationRoot BuildAppConfig()
	//{
	//	var binPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
	//	var config = new ConfigurationBuilder();
	//	config.SetBasePath(Path.Combine(binPath!, "../config"))
	//				.AddJsonFile("appsettings.json", optional: false)
	//				.AddCommandLine(Environment.GetCommandLineArgs());
	//	return config.Build();
	//}

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
}