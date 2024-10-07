using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ScriptingSurvey.Helpers;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace ScriptingSurvey;
static class Test2
{
	public static async void Run()
	{
		var samplesPath = PathHelper.FindDirectory("./##/scripts samples");
		if (samplesPath == null)
		{
			Console.WriteLine("Directory 'scripts samples' not found");
			return;
		}
		var fileName = Path.Combine(samplesPath, "Test2.cs");
		if (!File.Exists(fileName))
		{
			Console.WriteLine($"File '{fileName}' not exists");
			return;
		}

		var options = ScriptOptions.Default
					.AddImports("System",
							"System.IO",
							"System.Collections.Generic",
							"System.Console")
					.AddReferences("System",
							"ScriptingSurvey")
					.WithEmitDebugInformation(true)
					.WithFilePath(fileName)
					.WithFileEncoding(Encoding.UTF8);

		var scriptCode = File.ReadAllText(fileName);

		var sw = Stopwatch.StartNew();

		var script = CSharpScript.Create<TestBase>(scriptCode, options);
		script.Compile();
		Console.WriteLine(@"Compile time: {0}ms", sw.ElapsedMilliseconds);
		sw.Restart();
		var state = await script.RunAsync();
		var testScript = state.ReturnValue;
		Console.WriteLine(@"Execution time: {0}ms", sw.ElapsedMilliseconds);

		Console.WriteLine("Script result: {0};", testScript.Mul(2.6, 3.4));
		Console.WriteLine("Script result: {0};", testScript.Mul(2.6, 2));
		Console.WriteLine("Script result: {0};", testScript.Mul(10, 3.4));
	}
}
