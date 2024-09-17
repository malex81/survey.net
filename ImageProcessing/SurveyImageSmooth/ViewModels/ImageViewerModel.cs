using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using ImageProcessing.SurveyImageSmooth.Config;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;

namespace ImageProcessing.SurveyImageSmooth.ViewModels;

record ImageItem(string Title, Bitmap ImageSource);

internal partial class ImageViewerModel : ObservableObject
{
	#region static part
	public static readonly ImageViewerModel DesignModel = new("./##/image samples");
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
	#endregion

	[ObservableProperty]
	private ImageItem? selectedImage;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Zoom))]
	private double zoomRatio = 0;
	[ObservableProperty]
	private double rotateAngle = 0;

	public ImageItem[] Images { get; }
	public double Zoom => Math.Pow(2, ZoomRatio);

	ImageViewerModel(string relPath)
	{
		var samplesPath = PathHelper.FindDirectory(relPath);
		Images = samplesPath == null ? [] : LoadImages(samplesPath).ToArray();
		SelectedImage = Images.FirstOrDefault();
	}

	public ImageViewerModel(IOptions<ImagesOptions> imageOptions) : this(imageOptions.Value.SamplesPath ?? "")
	{
	}
}
