namespace ScriptingSurvey;

public static class TestDataProvider
{
	public static string SomeText => $"Current time: {DateTime.Now:HH:mm:ss}";
}

public record ScriptResult(string SomeText, DateTime Time);

public abstract class TestBase
{
	public abstract double Mul(double a, double b);
}