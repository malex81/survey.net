using ImageProcessing.Engine;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text;

namespace ImageProcessing.Helpers;

internal static class SysLog
{
	public static void TryLog(Exception ex, string fileName)
	{
		var kernel = App.CurrentKernel;
		if (kernel == null) return;
		try
		{
			var logConfigs = kernel.AppConfig.GetSection("Logging");
			var serilogConfig = logConfigs.GetSection("Serilog");
			var pathFormat = serilogConfig.GetValue<string>("PathFormat");
			if (pathFormat == null) return;
			var dir = Path.GetDirectoryName(pathFormat) ?? "./";
			var filePath = Path.Combine(dir, fileName);
			File.WriteAllText(filePath, ex.FormatException());
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
