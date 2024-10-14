#if !TESTS
using Avalonia;
using ImageProcessing.Helpers;
using System;
using System.Threading.Tasks;

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
			.LogToTrace();
}
#endif