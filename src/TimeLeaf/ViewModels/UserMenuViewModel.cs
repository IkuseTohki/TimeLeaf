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

    public UserMenuViewModel(
        IIdentityService identityService,
        IDialogService dialogService)
    {
        _identityService = identityService;
        _dialogService = dialogService;
    }

    [RelayCommand]
    private async Task EditProfile()
    {
        var profileVm = new ProfileEditViewModel(_identityService);
        await profileVm.LoadAsync();
        await _dialogService.ShowDialogAsync(profileVm);
    }
}
