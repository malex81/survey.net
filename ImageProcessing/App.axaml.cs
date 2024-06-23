using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageProcessing.Engine;
using System;

namespace ImageProcessing;

public partial class App : Application
{
    private EngineKernel? kernel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.Exit += AppExit;
			desktop.MainWindow = BuildMainWindow();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void AppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		kernel?.Dispose();
	}

	static Window BuildMainWindow()
	{
		

		return new MainWindow();
	}
}