using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageProcessing.SurveyCurves.ViewModels;

public record UniformDataSample(string Name, double[] Data);

public partial class ChartModel : ObservableObject
{
	public static readonly UniformDataSample[] InputSamples = [
		new("Единицы", [1, 1, 1, 1, 1, 1, 1, 1, 1, 1]),
		new("Простой", [1, 1, 1, 5, 5, 5, 4, 3, 2, 1, 2, 1, 6, 1]),
	];

	[ObservableProperty]
	private UniformDataSample currentSample = InputSamples[1];

	public ChartModel()
	{
	}
}
