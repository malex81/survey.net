using Avalonia.Controls;
using ImageProcessing.Base;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessing.SurveyCurves;

internal class ComponentRegistry : ISurveyComponent
{
	private Views.ChartControl? chartControl;

	public string Code => "Curves";
	public string Title => "Графики";

	public Control View => chartControl ??= new();

	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
	}
}
