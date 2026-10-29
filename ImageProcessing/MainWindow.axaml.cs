using Avalonia.Controls;
using ImageProcessing.MainViewModels;

namespace ImageProcessing;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += (_, _) => ((MainWindowModel?)DataContext)?.SelectFirst();
    }
}