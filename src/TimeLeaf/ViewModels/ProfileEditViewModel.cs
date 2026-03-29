using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ユーザー自身のプロフィールを編集するためのViewModel。
/// </summary>
public partial class ProfileEditViewModel : ObservableObject, LeafKit.UI.Services.IDialogViewModel
{
    private readonly IIdentityService _identityService;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _themeColor = "#2D5A27";

    [ObservableProperty]
    private string _iconPath = string.Empty;

    /// <summary>
    /// ダイアログを閉じるよう要求するイベント。
    /// </summary>
    public event Action<bool>? RequestClose;

    public ProfileEditViewModel(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    /// <summary>
    /// 現在のアイデンティティをロードします。
    /// </summary>
    public async Task LoadAsync()
    {
        var identity = await _identityService.GetCurrentIdentityAsync();
        DisplayName = identity.DisplayName;
        ThemeColor = identity.ThemeColor;
        IconPath = identity.IconPath;
    }

    /// <summary>
    /// 変更を保存してダイアログを閉じます。
    /// </summary>
    [RelayCommand]
    private async Task Save()
    {
        await _identityService.UpdateIdentityAsync(DisplayName, ThemeColor, IconPath);
        RequestClose?.Invoke(true);
    }

    /// <summary>
    /// キャンセルしてダイアログを閉じます。
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(false);
    }
}
