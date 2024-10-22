using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageProcessing.Engine;
using ImageProcessing.MainViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace ImageProcessing;

public partial class App : Application
{
	public static EngineKernel? CurrentKernel => ((App?)Current)?.kernel;

	private EngineKernel? kernel;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
			desktop.Exit += AppExit;
			desktop.MainWindow = BuildMainWindow();
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void AppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
	{
		kernel?.Dispose();
	}

	MainWindow BuildMainWindow()
	{
		kernel = EngineKernel.ConfigureBuilder(config =>
		{
			var binPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location);
			config.SetBasePath(Path.Combine(binPath!, "../config"))
				  .AddJsonFile("appsettings.json", optional: false)
				  .AddCommandLine(Environment.GetCommandLineArgs());
		}).AddServices(AppServicesConfig.LoggingConfig)
			.AddServices(AppServicesConfig.LocalOptions)
			.AddServices(AppServicesConfig.MainModels)
			.AddServices(AppServicesConfig.Surveys)
			.Build();

		return new() { DataContext = kernel.Services.GetRequiredService<MainWindowModel>() };
	}
}