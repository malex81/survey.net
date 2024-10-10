using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Helpers;
using ImageProcessing.MouseTools;
using ImageProcessing.RenderingMath;
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
record struct SelectionItem<T>(string Name, T Value);

internal partial class ImageViewerModel : ObservableObject, IMouseDragModel
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
	public static SelectionItem<PrefilterType>[] PrefilterItems => [
		new("None", PrefilterType.None),
		new("Find edges", PrefilterType.FindEdges),
		new("Gaussian blur", PrefilterType.GausianBlur),
	];
	public static SelectionItem<InterpolationType>[] InterpolationItems => [
		new("None", InterpolationType.None),
		new("Bilinear", InterpolationType.Bilinear),
		new("Bicubic", InterpolationType.Biсubic),
		new("Half Square B-Spline", InterpolationType.BSpline1_5),
		new("Square B-Spline", InterpolationType.BSpline2),
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
	private SelectionItem<PrefilterType> prefilter = PrefilterItems[0];
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private SelectionItem<InterpolationType> interpolation = InterpolationItems[0];
	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(DrawParams))]
	private Vector2 imageShift = new();

	public ImageItem[] Images { get; }
	public float Zoom => MathF.Pow(2, ZoomRatio);
	public BitmapDrawParams DrawParams => new(GetTransform(), Prefilter.Value, Interpolation.Value);

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

		return Matrix3x2.CreateTranslation(-(imgSize / 2).ToVector().Round())
			* Matrix3x2.CreateScale(Zoom)
			* Matrix3x2.CreateRotation(RotateAngle * MathF.PI / 180)
			* Matrix3x2.CreateTranslation((ViewBounds.Size / 2).ToVector() + ImageShift);
	}

	DragState? lastDrag;
	public void DragStart(DragState dragState)
	{
		lastDrag = dragState;
	}

	public void DragProcess(DragState dragState)
	{
		if (lastDrag == null || lastDrag.MouseButton != MouseButton.Left) return;
		var move = dragState.CurrentPos - lastDrag.CurrentPos;
		ImageShift += move.ToVector();
		lastDrag = dragState;
	}
}
