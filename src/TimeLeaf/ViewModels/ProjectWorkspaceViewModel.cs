using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のタスク管理（ワークスペース）を担当するViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly Project _project;

    public IEnumerable<TimeLeaf.Models.Enums.TaskStatus> TaskStatusValues => Enum.GetValues<TimeLeaf.Models.Enums.TaskStatus>();
    public IEnumerable<TaskPriority> TaskPriorityValues => Enum.GetValues<TaskPriority>();

    [ObservableProperty]
    private string _newTaskName = string.Empty;

    [ObservableProperty]
    private string _newTaskDescription = string.Empty;

    [ObservableProperty]
    private TimeLeaf.Models.Enums.TaskStatus _newTaskStatus = TimeLeaf.Models.Enums.TaskStatus.NotStarted;

    [ObservableProperty]
    private TaskPriority _newTaskPriority = TaskPriority.Medium;

    /// <summary>
    /// 表示対象となるタスクのリスト。
    /// </summary>
    public ObservableCollection<Task> Tasks => _project.Tasks;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="project">管理対象となるプロジェクト。</param>
    public ProjectWorkspaceViewModel(Project project)
    {
        _project = project;
    }

    /// <summary>
    /// 新規タスクを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskName)) return;

        var task = new Task
        {
            Name = NewTaskName,
            Description = NewTaskDescription,
            Status = NewTaskStatus,
            Priority = NewTaskPriority
        };
        Tasks.Add(task);

        NewTaskName = string.Empty;
        NewTaskDescription = string.Empty;
        NewTaskStatus = TimeLeaf.Models.Enums.TaskStatus.NotStarted;
        NewTaskPriority = TaskPriority.Medium;
    }
}
