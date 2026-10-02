#if !TESTS
using Avalonia;
using ImageProcessing.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace ImageProcessing;

class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		try
		{
			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}
		catch (Exception ex) { SysLog.TryLog(ex, "unhandled_main.txt"); }
	}

	private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
		=> SysLog.TryLog(e.Exception, "unobserved_task.txt");

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace()
			.AfterSetup(_ => InitializeCef());

	static void InitializeCef()
	{
		// CEF must be initialized through CefRuntimeLoader, never by calling
		// CefRuntime.Load/ExecuteProcess/Initialize directly. It resolves the subprocess
		// executable (CefGlueBrowserProcess\Xilium.CefGlue.BrowserProcess.exe), selects the
		// message loop mode for the current platform, registers CefRuntime.Shutdown on process
		// exit and stores the OSR flag that AvaloniaCefBrowser reads to pick its adapter.
		// The actual CefRuntime.Initialize runs lazily, on the first AvaloniaCefBrowser created.
		try
		{
			CefRuntimeLoader.Initialize(new CefSettings
			{
				RootCachePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"ImageProcessing", "cef"),

				// The NuGet package deploys locales under runtimes\<rid>\native\locales, while CEF
				// probes <app dir>\locales by default, so the directory has to be stated explicitly.
				LocalesDirPath = ResolveLocalesDir(AppContext.BaseDirectory),
				Locale = "ru",

				// Windowed rendering: CEF creates a real child HWND that Avalonia hosts through a
				// NativeControlHost. This is the configuration the official CefGlue Avalonia demo
				// uses; the off-screen (OSR) path of CefGlue.Avalonia 120.6099.215 allocates the
				// WriteableBitmap but never blits CEF frames into it, so the surface stays blank.
				WindowlessRenderingEnabled = false
			});
		}
		catch (Exception ex) { SysLog.TryLog(ex, "cef_init.txt"); }
	}

	static string ResolveLocalesDir(string basePath)
	{
		string[] candidates =
		[
			Path.Combine(basePath, "locales"),
			Path.Combine(basePath, "runtimes", "win-x64", "native", "locales"),
			Path.Combine(basePath, "runtimes", "win-arm64", "native", "locales")
		];

		return candidates.FirstOrDefault(d => File.Exists(Path.Combine(d, "en-US.pak"))) ?? candidates[0];
	}
}
#endif