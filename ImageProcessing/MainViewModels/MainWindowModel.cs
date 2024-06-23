using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageProcessing.Base;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace ImageProcessing.MainViewModels;

public class SurveyItemModel(ISurveyComponent survey)
{
    private readonly ISurveyComponent survey = survey;

    public string Title => survey.Title;
    public Control View => survey.View;
}

public partial class MainWindowModel : ObservableObject
{
    private readonly ILogger<MainWindowModel> logger;

	[ObservableProperty]
	SurveyItemModel? selectedItem;

	public SurveyItemModel[] Items { get; }

    public MainWindowModel(ILogger<MainWindowModel> logger, IEnumerable<ISurveyComponent> surveys)
    {
        this.logger = logger;
        logger.LogTrace("Run app...");

        Items = surveys.Select(s => new SurveyItemModel(s)).ToArray();
        SelectedItem = Items.FirstOrDefault();
    }
}
