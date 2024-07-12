using Avalonia.Controls;
using ImageProcessing.Base;
using ImageProcessing.SurveyImageSmooth.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessing.SurveyImageSmooth;

internal class ComponentRegistry(ImageSetModel viewModel) : ISurveyComponent
{
	private Views.ImageViewer? imageViewer;

	public string Code => "Images";
	public string Title => "Изображения";

	public Control View => imageViewer ??= new() { DataContext = viewModel };

	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
		services.AddSingleton<ImageSetModel>();
	}
}
