using System;
using System.Windows;

namespace TimeLeaf.Views;

/// <summary>
/// 致命的なエラーが発生した際に詳細を表示し、コピー可能にするためのウィンドウ。
/// </summary>
public partial class FatalErrorWindow : Window
{
    public FatalErrorWindow(string errorDetail)
    {
        InitializeComponent();
        ErrorDetailText.Text = errorDetail;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(ErrorDetailText.Text);
            MessageBox.Show("エラー内容をクリップボードにコピーしました。", "コピー完了", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch
        {
            // クリップボード操作に失敗しても何もしない
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
