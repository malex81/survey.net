using Avalonia.Controls;
using ImageProcessing.Base;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ImageProcessing.SurveyCef;

internal sealed class ComponentRegistry : ISurveyComponent, IDisposable
{
	public static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<ISurveyComponent, ComponentRegistry>();
	}

	private Views.WebViewer? mainControl;

	public string Code => "Chromium";
	public string Title => "Survey CIF";
	
	public Control View => mainControl ??= new();

	public void Dispose()
	{
		(mainControl as IDisposable)?.Dispose();
	}
}