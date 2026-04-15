using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 致命的なエラー表示画面のロジックを管理するViewModel。
/// </summary>
public partial class FatalErrorViewModel : ObservableObject
{
    /// <summary>
    /// エラーの詳細内容。
    /// </summary>
    [ObservableProperty]
    private string _errorDetail = string.Empty;

    /// <summary>
    /// エラー内容をクリップボードにコピーします。
    /// </summary>
    [RelayCommand]
    private void CopyErrorDetail()
    {
        try
        {
            Clipboard.SetText(ErrorDetail);
            MessageBox.Show(
                "エラー内容をクリップボードにコピーしました。",
                "コピー完了",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
        catch
        {
            // クリップボード操作に失敗しても何もしない
        }
    }
}
