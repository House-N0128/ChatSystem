using System.Windows;
using ChatSystem.Wpf.ViewModels;

namespace ChatSystem.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
