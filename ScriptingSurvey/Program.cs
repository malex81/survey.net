// https://habr.com/ru/articles/553310/

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ScriptingSurvey;
using System.Diagnostics;

var options = ScriptOptions.Default
			.AddImports("System",
					"System.IO",
					"System.Collections.Generic",
					"System.Console", "ScriptingSurvey")
			.AddReferences("System",
					"ScriptingSurvey");

var scriptCode = @"
	var text = TestDataProvider.SomeText;
	Console.WriteLine(""Hello from script. {0}"", text);
	return new ScriptResult(text, DateTime.Now);
";

var sw = Stopwatch.StartNew();

var script = CSharpScript.Create<ScriptResult>(scriptCode, options);
script.Compile();
Console.WriteLine(@"Compile time: {0}ms", sw.ElapsedMilliseconds);
sw.Restart();
var state = await script.RunAsync();
Console.WriteLine(@"Execution time: {0}ms", sw.ElapsedMilliseconds);

Console.WriteLine("Script result text: {0}; script time: {1:HH:mm:ss}", state.ReturnValue.SomeText, state.ReturnValue.Time);