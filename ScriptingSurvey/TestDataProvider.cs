namespace ScriptingSurvey;

public static class TestDataProvider
{
	public static string SomeText => $"Current time: {DateTime.Now:HH:mm:ss}";
}

public record ScriptResult(string SomeText, DateTime Time);