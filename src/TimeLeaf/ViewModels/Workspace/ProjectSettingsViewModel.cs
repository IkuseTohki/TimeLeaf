using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトの設定（名称変更、アーカイブ等）を担当するViewModel。
/// </summary>
public partial class ProjectSettingsViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IProjectRepository _projectRepository;
    private readonly IIdentityService _identityService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;

    public ProjectSettingsViewModel(
        ProjectViewModel projectViewModel,
        IProjectRepository projectRepository,
        IIdentityService identityService,
        INotificationService notificationService,
        IDialogService dialogService
    )
    {
        _projectViewModel = projectViewModel;
        _projectRepository = projectRepository;
        _identityService = identityService;
        _notificationService = notificationService;
        _dialogService = dialogService;
    }

    public bool IsArchived => _projectViewModel.Model.IsArchived;
    public string ProjectName => _projectViewModel.Name;

    [RelayCommand]
    private async Task Archive()
    {
        var result = MessageBox.Show(
            "プロジェクトをアーカイブしますか？\nアーカイブすると変更ができなくなり、一覧で非表示になります（アーカイブフィルタで表示可能）。",
            "アーカイブの確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            _projectViewModel.Model.Archive();
            await _projectRepository.SaveAsync(_projectViewModel.Model, _identityService.CurrentUserId.ToString());
            OnPropertyChanged(nameof(IsArchived));
            _notificationService.Notify(
                new Notification(
                    "プロジェクト更新",
                    "プロジェクトをアーカイブしました。",
                    _projectViewModel.Id.ToString()
                )
            );
        }
    }

    [RelayCommand]
    private async Task Unarchive()
    {
        _projectViewModel.Model.Unarchive();
        await _projectRepository.SaveAsync(_projectViewModel.Model, _identityService.CurrentUserId.ToString());
        OnPropertyChanged(nameof(IsArchived));
        _notificationService.Notify(
            new Notification(
                "プロジェクト更新",
                "プロジェクトのアーカイブを解除しました。",
                _projectViewModel.Id.ToString()
            )
        );
    }

    [RelayCommand]
    private async Task Delete()
    {
        // 削除確認は慎重に
        var result = MessageBox.Show(
            $"本当にプロジェクト「{ProjectName}」を削除しますか？\nこの操作は取り消せません。プロジェクトフォルダごと完全に削除されます。",
            "プロジェクト削除の確認",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            await _projectRepository.DeleteAsync(_projectViewModel.Model.Id);
            _notificationService.Notify(
                new Notification("プロジェクト削除", "プロジェクトを削除しました。", string.Empty)
            );

            // ホームに戻る等のナビゲーションが必要だが、
            // ProjectChangedイベントにより上位でリロードが走り、
            // 存在しないプロジェクトを参照している状態になるとエラーになる可能性がある。
            // ここでは単に削除を実行し、上位層（MainViewModel）が変更通知を受けて適切に遷移することを期待する。
        }
    }
}
