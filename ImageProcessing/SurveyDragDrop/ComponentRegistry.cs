using Avalonia.Controls;
using ImageProcessing.Base;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ImageProcessing.SurveyDragDrop;

internal class ComponentRegistry : ISurveyComponent, IDisposable
{
	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
	}

	private Views.SandboxControl? mainControl;

	public string Code => "DragDrop";
	public string Title => "Drag & drop";

	public Control View => mainControl ??= new();

	public void Dispose()
	{
		(mainControl as IDisposable)?.Dispose();
	}
}
