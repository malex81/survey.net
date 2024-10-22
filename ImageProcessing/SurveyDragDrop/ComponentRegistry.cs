using Avalonia.Controls;
using ImageProcessing.Base;
using Microsoft.Extensions.DependencyInjection;

namespace ImageProcessing.SurveyDragDrop;

internal class ComponentRegistry : ISurveyComponent
{
	private Views.SandboxControl? mainControl;

	public string Code => "DragDrop";
	public string Title => "Drag & drop";

	public Control View => mainControl ??= new();

	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
	}
}
