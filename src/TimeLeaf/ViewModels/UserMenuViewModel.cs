using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ユーザーメニューを制御する ViewModel。
/// プロフィール編集やその他のユーザー設定への入り口となります。
/// </summary>
public partial class UserMenuViewModel : ObservableObject
{
    private readonly IIdentityService _identityService;
    private readonly IUserService _userService;
    private readonly IDialogService _dialogService;
    private readonly IViewModelFactory _viewModelFactory;

    [ObservableProperty]
    private string _userName = "...";

    [ObservableProperty]
    private string _userInitial = "?";

    public UserMenuViewModel(
        IIdentityService identityService,
        IUserService userService,
        IDialogService dialogService,
        IViewModelFactory viewModelFactory
    )
    {
        _identityService = identityService;
        _userService = userService;
        _dialogService = dialogService;
        _viewModelFactory = viewModelFactory;

        // ユーザー情報の変更を購読
        _userService.UserChanged += OnUserChanged;

        // 初期情報をロード（非同期）
        _ = LoadIdentityAsync();
    }

    private async System.Threading.Tasks.Task LoadIdentityAsync()
    {
        var identity = await _identityService.GetCurrentIdentityAsync();
        UpdateIdentity(identity);
    }

    private void OnUserChanged(Models.Entities.User user)
    {
        if (user.Id == _identityService.CurrentUserId)
        {
            UpdateIdentity(user);
        }
    }

    private void UpdateIdentity(Models.Entities.User user)
    {
        UserName = user.DisplayName;
        UserInitial = string.IsNullOrEmpty(UserName) ? "?" : UserName.Substring(0, 1);
    }

    public event System.Action? CloseRequested;

    [RelayCommand]
    private async System.Threading.Tasks.Task EditProfile()
    {
        CloseRequested?.Invoke();
        var profileVm = _viewModelFactory.CreateProfileEditViewModel();
        await profileVm.LoadAsync();
        await _dialogService.ShowDialogAsync(profileVm);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task OpenSettings()
    {
        CloseRequested?.Invoke();
        var settingsVm = _viewModelFactory.CreateApplicationSettingsViewModel();
        await _dialogService.ShowDialogAsync(settingsVm);
    }
}
