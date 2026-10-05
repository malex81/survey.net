using ImageProcessing.Engine;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;

namespace ImageProcessing.Helpers;

internal static class SysLog
{
	public static void TryLog(Exception ex, string fileName)
		=> TryLog(ex.FormatException(), fileName);

	public static void TryLog(string message, string fileName)
	{
		string? dir = ResolveLogDir();
		if (dir == null)
		{
			TryLogFallback(fileName, message);
			return;
		}

		try
		{
			Directory.CreateDirectory(dir);
			File.WriteAllText(Path.Combine(dir, fileName), message);
		}
		catch
		{
			TryLogFallback(fileName, message);
		}
	}

	static string? ResolveLogDir()
	{
		try
		{
			string? binPath = Path.GetDirectoryName(typeof(SysLog).Assembly.Location);
			if (string.IsNullOrEmpty(binPath)) return null;

			IConfigurationRoot cfg = new ConfigurationBuilder()
				.SetBasePath(Path.Combine(binPath, "..", "config"))
				.AddJsonFile("appsettings.json", optional: false)
				.AddCommandLine(Environment.GetCommandLineArgs())
				.Build();

			string? pathFormat = cfg["Logging:Serilog:PathFormat"];
			return pathFormat == null ? null : Path.GetDirectoryName(pathFormat);
		}
		catch
		{
			return null;
		}
	}

	static void TryLogFallback(string fileName, string message)
	{
		try
		{
			string dir = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ImageProcessing");
			Directory.CreateDirectory(dir);
			File.WriteAllText(Path.Combine(dir, fileName), message);
		}
		catch { }
	}

	static string FormatException(this Exception ex)
	{
		StringBuilder msg = new();
		string? stack = null;
		var _ex = ex;
		while (_ex != null)
		{
			stack ??= _ex.StackTrace;
			msg.AppendLine($"╠══> {_ex.Message}");
			_ex = _ex.InnerException;
		}
		if (stack != null)
		{
			msg.AppendLine("╚════ Stack ════");
			msg.AppendLine(stack);
		}
		return msg.ToString();
	}

}
