using ScriptingSurvey;

internal class Main : TestBase
{
	private readonly TestDataProvider2 dataProvider;

	public Main(TestDataProvider2 dataProvider)
	{
		this.dataProvider = dataProvider;
	}

	public override double Mul(double a, double b)
	{
		return a * b + dataProvider.GetOffset();
	}
}