using ImageProcessing.MainViewModels;
using ImageProcessing.SurveyImageSmooth.Config;
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

	public static void LocalOptions(IServiceCollection services, IConfigurationRoot appConfig)
	{
		services.Configure<ImagesOptions>(appConfig.GetSection("Images"));
	}

	public static void MainModels(IServiceCollection services)
	{
		services.AddSingleton<MainWindowModel>();
	}

	public static void Surveys(IServiceCollection services)
	{
		SurveyImageSmooth.ComponentRegistry.RegisterServices(services);
		SurveyCurves.ComponentRegistry.RegisterServices(services);
	}
}
