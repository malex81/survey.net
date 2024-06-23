using Avalonia.Controls;
using ImageProcessing.Base;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessing.SurveyImageSmoth;

internal class ComponentRegistry : ISurveyComponent
{
	private Views.ImageViewer? imageViewer;

	public string Code => "Images";
	public string Title => "Изображения";

	public Control View => imageViewer ??= new();

	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
	}
}
