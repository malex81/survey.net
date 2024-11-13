using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Diagnostics;

namespace ScriptingSurvey;

public record Test1InputParams(string Text);

static class Test1
{
	public static async Task Run()
	{
		var options = ScriptOptions.Default
					.AddImports("System",
							"System.IO",
							"System.Collections.Generic",
							"System.Console",
							"ScriptingSurvey")
					.AddReferences("System",
							"ScriptingSurvey");

		var scriptCode = @"
	var text = ""[Global]"" + TestDataProvider.SomeText + ""[Params]"" + Text;
	Console.WriteLine(""Hello from script. {0}"", text);
	return new ScriptResult(text, DateTime.Now);
";

		var sw = Stopwatch.StartNew();

		var script = CSharpScript.Create<ScriptResult>(scriptCode, options, typeof(Test1InputParams));
		script.Compile();
		Console.WriteLine(@"Compile time: {0}ms", sw.ElapsedMilliseconds);
		sw.Restart();
		var state = await script.RunAsync(new Test1InputParams("Text from params"));
		Console.WriteLine(@"Execution time: {0}ms", sw.ElapsedMilliseconds);

		Console.WriteLine("Script result text: {0}; script time: {1:HH:mm:ss}", state.ReturnValue.SomeText, state.ReturnValue.Time);
	}
}
