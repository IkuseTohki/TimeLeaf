using System.Windows;
using LeafKit.UI.Services;

namespace TimeLeaf.Views;

/// <summary>
/// MainWindow.xaml の相互作用ロジック。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ISnackbarService snackbarService)
    {
        InitializeComponent();
        Snackbar.Initialize(snackbarService);
    }
}
