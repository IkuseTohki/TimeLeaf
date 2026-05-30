using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ユーザー管理を行うダイアログの ViewModel。
/// </summary>
public partial class UserManagementViewModel : ObservableObject, IDialogViewModel
{
    private readonly IUserService _userService;
    private readonly IIdentityService _identityService;
    private readonly IDialogService _dialogService;

    public ObservableCollection<UserItemViewModel> Users { get; } = new();

    /// <inheritdoc />
    public event Action<bool>? RequestClose;

    public UserManagementViewModel(
        IUserService userService,
        IIdentityService identityService,
        IDialogService dialogService
    )
    {
        _userService = userService;
        _identityService = identityService;
        _dialogService = dialogService;
    }

    /// <summary>
    /// ユーザー一覧をロードします。
    /// </summary>
    public async Task LoadAsync()
    {
        var users = await _userService.GetActiveUsersAsync();
        Users.Clear();

        // 自分を先頭にし、他者を名前順で並べる
        var sortedUsers = users
            .OrderByDescending(u => u.Id == _identityService.CurrentUserId)
            .ThenBy(u => u.DisplayName);

        foreach (var user in sortedUsers)
        {
            var isMe = user.Id == _identityService.CurrentUserId;
            Users.Add(new UserItemViewModel(user, isMe, DeleteUserAsync));
        }
    }

    private async Task DeleteUserAsync(UserItemViewModel userVm)
    {
        var confirmed = _dialogService.ShowConfirmationDialog(
            $"{userVm.DisplayName} を削除してもよろしいですか？\nこの操作はファイル共有を通じて他メンバーにも波及します。",
            "ユーザーの削除"
        );

        if (confirmed)
        {
            await _userService.DeleteUserAsync(userVm.Id);
            Users.Remove(userVm);
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke(false);
    }
}

/// <summary>
/// ユーザー管理画面の各行を表す ViewModel。
/// </summary>
public partial class UserItemViewModel : ObservableObject
{
    private readonly Func<UserItemViewModel, Task> _deleteAction;

    public Guid Id { get; }
    public string DisplayName { get; }
    public string UserInitial { get; }
    public string ThemeColor { get; }
    public bool IsMe { get; }

    public UserItemViewModel(User user, bool isMe, Func<UserItemViewModel, Task> deleteAction)
    {
        Id = user.Id;
        DisplayName = user.DisplayName;
        UserInitial = string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName.Substring(0, 1);
        ThemeColor = user.ThemeColor;
        IsMe = isMe;
        _deleteAction = deleteAction;
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        await _deleteAction(this);
    }

    private bool CanDelete() => !IsMe;
}
