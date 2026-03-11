using System.Windows;
using LeafKit.UI.Services;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Views;

/// <summary>
/// MainWindow.xaml の相互作用ロジック。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(ISnackbarService snackbarService, IDispatcherService dispatcherService)
    {
        InitializeComponent();
        Snackbar.Initialize(snackbarService, dispatcherService);

        this.Loaded += (s, e) =>
        {
            if (this.DataContext is MainViewModel viewModel)
            {
                // トレイメニューからの完全終了要求をハンドル
                viewModel.ExitRequested += (sender, args) =>
                {
                    Application.Current.Shutdown();
                };
            }
        };
    }
}
