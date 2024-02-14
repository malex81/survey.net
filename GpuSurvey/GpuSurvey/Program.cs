using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GpuSurvey;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeviceSurvey
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			var services = new ServiceCollection();
			ConfigureServices(services);
			var serviceProvider = services.BuildServiceProvider();
			var surveyItems = serviceProvider.GetService<IEnumerable<ISurveyArea>>();
			var logger = serviceProvider.GetService<ILogger<Program>>();
			foreach (var item in surveyItems)
			{
				try
				{
					logger.LogInformation("========== {surveyName} ==========", item.Name);
					item.Survey();
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, "Survey area {surveyType} failed", item.GetType().Name);
				}
			}

			await Task.Delay(100);
		}

		private static void ConfigureServices(IServiceCollection services)
		{
			services.AddLogging(logBuilder => logBuilder
						.AddFilter("Default", LogLevel.Trace)
						.AddFilter("Microsoft", LogLevel.Warning)
						.AddConsole()
						.AddDebug());
			
			services.AddTransient<ISurveyArea, InvestigateDevice>();
		}
	}
}
