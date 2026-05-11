using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// マイルストーンを追加するための ViewModel です。
/// </summary>
public partial class AddMilestoneViewModel : ObservableObject, IDialogViewModel
{
    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private string _label = string.Empty;

    /// <summary>
    /// ダイアログのタイトル。
    /// </summary>
    public string Title => "マイルストーンの追加";

    /// <summary>
    /// ダイアログを閉じることを要求するイベント。
    /// </summary>
    public event Action<bool>? RequestClose;

    public AddMilestoneViewModel() { }

    /// <summary>
    /// 追加を確定するコマンド。
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Label))
            return;

        RequestClose?.Invoke(true);
    }

    /// <summary>
    /// キャンセルするコマンド。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
