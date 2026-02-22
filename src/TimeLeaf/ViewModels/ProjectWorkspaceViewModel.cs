using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のタスク管理（ワークスペース）を担当するViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel; // ProjectViewModel に変更
    private readonly ILogger<ProjectWorkspaceViewModel> _logger;

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

    [ObservableProperty]
    private DateTime? _newTaskDeadline;

    [ObservableProperty]
    private double _newTaskEstimatedCost;

    [ObservableProperty]
    private double _newTaskActualCost;

    /// <summary>
    /// 表示対象となるタスクのリスト。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks; // ProjectViewModel の Tasks プロパティを参照

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="project">管理対象となるプロジェクト。</param>
    /// <param name="logger">ロガー。</param>
    public ProjectWorkspaceViewModel(Project project, ILogger<ProjectWorkspaceViewModel> logger) // Projectエンティティを受け取る
    {
        _projectViewModel = new ProjectViewModel(project); // ViewModelでラップ
        _logger = logger;
        _logger.LogInformation("ProjectWorkspaceViewModel initialized for project {ProjectId}.", _projectViewModel.Id);
    }

    /// <summary>
    /// 新規タスクを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddTask()
    {
        _logger.LogInformation("Attempting to add new task with name: {NewTaskName}", NewTaskName);
        if (string.IsNullOrWhiteSpace(NewTaskName))
        {
            _logger.LogWarning("Task name is empty. Cannot add task.");
            return;
        }

        try
        {
            var taskEntity = new ProjectTask // ProjectTaskエンティティを作成
            {
                Name = NewTaskName,
                Description = NewTaskDescription,
                Status = NewTaskStatus,
                Priority = NewTaskPriority,
                Deadline = NewTaskDeadline,
                EstimatedCost = NewTaskEstimatedCost,
                ActualCost = NewTaskActualCost
            };
            var taskViewModel = new ProjectTaskViewModel(taskEntity); // ViewModelでラップ
            Tasks.Add(taskViewModel); // ProjectTaskViewModel をコレクションに追加
            _logger.LogInformation("Task '{TaskName}' (ID: {TaskId}) added to project {ProjectId}.", taskViewModel.Name, taskViewModel.Id, _projectViewModel.Id);

            NewTaskName = string.Empty;
            NewTaskDescription = string.Empty;
            NewTaskStatus = TimeLeaf.Models.Enums.TaskStatus.NotStarted;
            NewTaskPriority = TaskPriority.Medium;
            NewTaskDeadline = null;
            NewTaskEstimatedCost = 0;
            NewTaskActualCost = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task with name: {NewTaskName} to project {ProjectId}.", NewTaskName, _projectViewModel.Id);
            // ここでUIにエラーを通知する等の処理を追加することも検討できます。
        }
    }
}
