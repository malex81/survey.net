using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using ImageProcessing.SurveyImageSmooth.Config;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace ImageProcessing.SurveyImageSmooth.ViewModels;

record ImageItem(string Title, Bitmap ImageSource);

internal partial class ImageSetModel : ObservableObject
{
	public static readonly ImageSetModel DesignModel = new("./##/image samples");
	static IEnumerable<ImageItem> LoadImages(string dirPath)
	{
		foreach (var filePath in Directory.EnumerateFiles(dirPath))
		{
			Bitmap? image = null;
			try
			{
				image = new Bitmap(filePath);
			}
			catch { }
			if (image != null)
				yield return new(Path.GetFileName(filePath), image);
		}
	}

	[ObservableProperty]
	private ImageItem? selectedImage;

	public ImageItem[] Images { get; }

	ImageSetModel(string relPath)
	{
		var samplesPath = PathHelper.FindDirectory(relPath);
		Images = samplesPath == null ? [] : LoadImages(samplesPath).ToArray();
		SelectedImage = Images.FirstOrDefault();
	}

	public ImageSetModel(IOptions<ImagesOptions> imageOptions) : this(imageOptions.Value.SamplesPath ?? "")
	{
	}
}
