using ImageProcessing.MainViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ImageProcessing.Engine;
public static class AppServicesConfig
{
	public static void LoggingConfig(IServiceCollection services, IConfigurationRoot appConfig)
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
	}

	public static void MainModels(IServiceCollection services)
	{
		services.AddSingleton<MainWindowModel>();
	}

	public static void Surveys(IServiceCollection services)
	{
		SurveyCurves.ComponentRegistry.RegisterServices(services);
		SurveyImageSmooth.ComponentRegistry.RegisterServices(services);
	}
}
