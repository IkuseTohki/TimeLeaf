using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Services;
using LeafKit.UI.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ユーザーメニューを制御する ViewModel。
/// プロフィール編集やその他のユーザー設定への入り口となります。
/// </summary>
public partial class UserMenuViewModel : ObservableObject
{
    private readonly IIdentityService _identityService;
    private readonly IDialogService _dialogService;
    private readonly IViewModelFactory _viewModelFactory;

    public UserMenuViewModel(
        IIdentityService identityService,
        IDialogService dialogService,
        IViewModelFactory viewModelFactory)
    {
        _identityService = identityService;
        _dialogService = dialogService;
        _viewModelFactory = viewModelFactory;
    }

    [RelayCommand]
    private async Task EditProfile()
    {
        var profileVm = _viewModelFactory.CreateProfileEditViewModel();
        await profileVm.LoadAsync();
        await _dialogService.ShowDialogAsync(profileVm);
    }

    [RelayCommand]
    private async Task OpenSettings()
    {
        var settingsVm = _viewModelFactory.CreateApplicationSettingsViewModel();
        await _dialogService.ShowDialogAsync(settingsVm);
    }
}
