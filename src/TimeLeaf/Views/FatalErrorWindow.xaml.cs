using System.Windows;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Views;

/// <summary>
/// 致命的なエラーが発生した際に詳細を表示し、コピー可能にするためのウィンドウ。
/// </summary>
public partial class FatalErrorWindow : Window
{
    public FatalErrorWindow(string errorDetail)
    {
        InitializeComponent();
        DataContext = new FatalErrorViewModel { ErrorDetail = errorDetail };
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
