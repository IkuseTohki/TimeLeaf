using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// タスクを追加するための ViewModel です。
/// </summary>
public partial class AddTaskViewModel : ObservableObject, IDialogViewModel, IDisposable
{
    private readonly IUserService? _userService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private TaskStatus _status = TaskStatus.NotStarted;

    [ObservableProperty]
    private TaskPriority _priority = TaskPriority.Medium;

    [ObservableProperty]
    private User? _assignee;

    [ObservableProperty]
    private ObservableCollection<User> _teammates = new();

    [ObservableProperty]
    private DateTime? _dueDate;

    [ObservableProperty]
    private DateTime? _plannedStartDate;

    [ObservableProperty]
    private double? _estimatedWorkHours;

    public AddTaskViewModel()
    {
        // デザイナー用デフォルトコンストラクタ
    }

    public AddTaskViewModel(IUserService userService)
    {
        _userService = userService;
        if (_userService != null)
        {
            _userService.UserChanged += OnUserChanged;
        }
    }

    private void OnUserChanged(User updatedUser)
    {
        // 候補リスト内のユーザーを更新
        var existing = Teammates.FirstOrDefault(u => u.Id == updatedUser.Id);
        if (existing != null)
        {
            var index = Teammates.IndexOf(existing);
            Teammates[index] = updatedUser;
        }

        // 選択中の担当者が更新された場合も差し替え
        if (Assignee?.Id == updatedUser.Id)
        {
            Assignee = updatedUser;
        }
    }

    public void Dispose()
    {
        if (_userService != null)
        {
            _userService.UserChanged -= OnUserChanged;
        }
    }

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
        if (string.IsNullOrWhiteSpace(Name))
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
