using Avalonia.Media;
using Avalonia.Media.Imaging;
using ImageProcessing.Helpers;
using ImageProcessing.SurveyImageSmooth.Config;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageProcessing.SurveyImageSmooth.ViewModels;

record ImageItem(string Title, IImage ImageSource);

internal class ImageSetModel
{
	public static readonly ImageSetModel DesignModel = new("./##/image samples");

	static IEnumerable<ImageItem> LoadImages(string dirPath)
	{
		foreach (var filePath in Directory.EnumerateFiles(dirPath))
		{
			IImage? image = null;
			try
			{
				image = new Bitmap(filePath);
			}
			catch { }
			if (image != null)
				yield return new(Path.GetFileName(filePath), image);
		}
	}

	public ImageItem[] Images { get; }

	ImageSetModel(string relPath)
	{
		var samplesPath = PathHelper.FindDirectory(relPath);
		Images = samplesPath == null ? [] : LoadImages(samplesPath).ToArray();
	}

	public ImageSetModel(IOptions<ImagesOptions> imageOptions) : this(imageOptions.Value.SamplesPath ?? "")
	{
	}
}
