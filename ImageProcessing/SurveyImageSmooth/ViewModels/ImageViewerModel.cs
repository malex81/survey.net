using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using ImageProcessing.SurveyCurves.ViewModels;
using ImageProcessing.SurveyImageSmooth.Config;
using ImageProcessing.SurveyImageSmooth.Engine;
using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reactive.Linq;

namespace ImageProcessing.SurveyImageSmooth.ViewModels;

record ImageItem(string Title, Bitmap ImageSource);
record struct SmoothInfo(string Name, SmoothType Smooth);

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

	public static SmoothInfo[] SmoothItems => [
		new("No smooth", SmoothType.None),
		new("Bilinear", SmoothType.Bilinear),
		new("Square B-Spline", SmoothType.BSpline2),
		new("Bicubic", SmoothType.Biсubic),
	];
	#endregion

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private ImageItem? selectedImage;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(Zoom))]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private float zoomRatio = 0;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private float rotateAngle = 0;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private Rect viewBounds;
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private SmoothInfo smoothInfo = SmoothItems[0];

	public ImageItem[] Images { get; }
	public float Zoom => MathF.Pow(2, ZoomRatio);
	public BitmapDrawParams DrawParams => new(GetTransform(), SmoothInfo.Smooth);

	ImageViewerModel(string relPath)
	{
		var samplesPath = PathHelper.FindDirectory(relPath);
		Images = samplesPath == null ? [] : LoadImages(samplesPath).ToArray();
		SelectedImage = Images.FirstOrDefault();
	}

	public ImageViewerModel(IOptions<ImagesOptions> imageOptions) : this(imageOptions.Value.SamplesPath ?? "")
	{
	}

	Matrix3x2 GetTransform()
	{
		if (SelectedImage == null) return Matrix3x2.Identity;
		var imgSize = SelectedImage.ImageSource.Size;

		return Matrix3x2.CreateTranslation(-(imgSize / 2).ToVector())
			* Matrix3x2.CreateScale(Zoom)
			* Matrix3x2.CreateRotation(RotateAngle * MathF.PI / 180)
			* Matrix3x2.CreateTranslation((ViewBounds.Size / 2).ToVector());
	}
}
