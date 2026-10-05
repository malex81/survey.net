#if !TESTS
using Avalonia;
using ImageProcessing.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Text;
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
		// The CEF subprocess host (CefGlueBrowserProcess\Xilium.CefGlue.BrowserProcess) must be
		// executable on Linux. Publishing from Windows (NTFS has no +x bit) and then copying to the
		// target often drops the execute permission, which makes libcef fail with
		// "LaunchProcess: failed to execvp" and the child process exit with code 127.
		EnsureCefSubprocessExecutable();

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

	static void EnsureCefSubprocessExecutable()
	{
		if (!OperatingSystem.IsLinux()) return;

		var log = new StringBuilder();
		log.AppendLine($"CEF subprocess chmod diagnostic @ {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

		string[] candidates =
		[
			Path.Combine(AppContext.BaseDirectory, "CefGlueBrowserProcess", "Xilium.CefGlue.BrowserProcess"),
			Path.Combine(AppContext.BaseDirectory, "Xilium.CefGlue.BrowserProcess")
		];

		foreach (string path in candidates)
		{
			try
			{
				log.AppendLine($"check: {path}");
				if (!File.Exists(path))
				{
					log.AppendLine("  not found");
					continue;
				}

				UnixFileMode before = File.GetUnixFileMode(path);
				log.AppendLine($"  before: {before}");

				File.SetUnixFileMode(path, before
					| UnixFileMode.UserExecute
					| UnixFileMode.GroupExecute
					| UnixFileMode.OtherExecute);

				log.AppendLine($"  after: {File.GetUnixFileMode(path)}");
			}
			catch (Exception ex)
			{
				log.AppendLine($"  ERROR {ex.GetType().Name}: {ex.Message}");
			}
		}

		SysLog.TryLog(log.ToString(), "cef-chmod.log");
	}
}
#endif