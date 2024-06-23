using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImageProcessing.MainViewModels;
public partial class MainWindowDesignModel : ObservableObject
{
	public class DesignItemModel(string title)
	{
		public string Title => title;
		public string View => $"Тут че-то как-то - {title}";
	}

	[ObservableProperty]
	DesignItemModel? selectedItem;

	public DesignItemModel[] Items { get; }
	
	public MainWindowDesignModel()
	{
		Items = [new DesignItemModel("test #1"), new DesignItemModel("test #2"), new DesignItemModel("test #3")];
	}
}
