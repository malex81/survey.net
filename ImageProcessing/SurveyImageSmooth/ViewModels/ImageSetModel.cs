using Avalonia.Media;

namespace ImageProcessing.SurveyImageSmooth.ViewModels;

record ImageItem(string Title, IImage ImageSource);

internal class ImageSetModel
{
	public ImageItem[] Images { get; }

    public ImageSetModel()
    {
    }
}
