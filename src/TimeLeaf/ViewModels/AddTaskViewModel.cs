using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// タスクを追加するための ViewModel です。
/// </summary>
public partial class AddTaskViewModel : ObservableObject, IDialogViewModel
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TaskStatus _status = TaskStatus.NotStarted;

    [ObservableProperty]
    private TaskPriority _priority = TaskPriority.Medium;

    [ObservableProperty]
    private string? _assignee;

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private double? _estimatedWorkHours;

    /// <summary>
    /// 選択可能なステータスのリスト。
    /// </summary>
    public IEnumerable<TaskStatus> TaskStatusValues => (TaskStatus[])Enum.GetValues(typeof(TaskStatus));

    /// <summary>
    /// 選択可能な優先度のリスト。
    /// </summary>
    public IEnumerable<TaskPriority> TaskPriorityValues => (TaskPriority[])Enum.GetValues(typeof(TaskPriority));

    /// <summary>
    /// ダイアログを閉じるよう要求するイベント。
    /// </summary>
    public event Action<bool>? RequestClose;

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
