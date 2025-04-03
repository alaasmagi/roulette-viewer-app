using Avalonia.Controls;
using WebApp.ViewModels;

namespace WebApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}