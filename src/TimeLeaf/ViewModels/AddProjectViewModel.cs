using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクト新規作成ダイアログのロジックを担当するViewModel。
/// </summary>
public partial class AddProjectViewModel : ObservableObject, IDialogViewModel
{
    public event Action<bool>? RequestClose;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private ProjectStatus _status = ProjectStatus.Initial;

    [ObservableProperty]
    private ProjectHealth _health = ProjectHealth.Healthy;

    public IEnumerable<ProjectStatus> ProjectStatusValues => (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));
    public IEnumerable<ProjectHealth> ProjectHealthValues => (ProjectHealth[])Enum.GetValues(typeof(ProjectHealth));

    /// <summary>
    /// 作成を確定するコマンド。
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
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
