using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using ImageProcessing.SurveyImageSmooth.Config;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
	[NotifyPropertyChangedFor(nameof(Transform))]
	private ImageItem? selectedImage;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Zoom))]
	[NotifyPropertyChangedFor(nameof(Transform))]
	private float zoomRatio = 0;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Transform))]
	private float rotateAngle = 0;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Transform))]
	private Rect viewBounds;

	public ImageItem[] Images { get; }
	public float Zoom => MathF.Pow(2, ZoomRatio);
	public Matrix3x2 Transform
	{
		get
		{
			if (SelectedImage == null) return Matrix3x2.Identity;
			var imgSize = SelectedImage.ImageSource.Size;

			return Matrix3x2.CreateTranslation(-(imgSize / 2).ToVector())
				* Matrix3x2.CreateScale(Zoom)
				* Matrix3x2.CreateRotation(RotateAngle * MathF.PI / 180)
				* Matrix3x2.CreateTranslation((ViewBounds.Size / 2).ToVector());
		}
	}

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
