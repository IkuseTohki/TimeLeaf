using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のタスク管理（ワークスペース）を担当するViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ICurrentUserService _userService;
    private readonly ILogger<ProjectWorkspaceViewModel> _logger;

    public IEnumerable<TimeLeaf.Models.Enums.TaskStatus> TaskStatusValues => (TimeLeaf.Models.Enums.TaskStatus[])Enum.GetValues(typeof(TimeLeaf.Models.Enums.TaskStatus));
    public IEnumerable<TaskPriority> TaskPriorityValues => (TaskPriority[])Enum.GetValues(typeof(TaskPriority));

    [ObservableProperty]
    private string _newTaskName = string.Empty;

    [ObservableProperty]
    private string _newTaskDescription = string.Empty;

    [ObservableProperty]
    private TimeLeaf.Models.Enums.TaskStatus _newTaskStatus = TimeLeaf.Models.Enums.TaskStatus.NotStarted;

    [ObservableProperty]
    private TaskPriority _newTaskPriority = TaskPriority.Medium;

    [ObservableProperty]
    private DateTime? _newTaskScheduledStartDate;

    [ObservableProperty]
    private DateTime? _newTaskDeadline;

    [ObservableProperty]
    private DateTime? _newTaskActualStartDate;

    [ObservableProperty]
    private DateTime? _newTaskActualEndDate;

    [ObservableProperty]
    private double _newTaskEstimatedCost;

    [ObservableProperty]
    private double _newTaskActualCost;

    [ObservableProperty]
    private string _newTaskAssignee = string.Empty;

    [ObservableProperty]
    private DateTime _newMilestoneDate = DateTime.Today;

    [ObservableProperty]
    private string _newMilestoneLabel = string.Empty;

    [ObservableProperty]
    private ProjectTaskViewModel? _selectedTask;

    [ObservableProperty]
    private string _newCommentContent = string.Empty;

    /// <summary>
    /// 管理対象プロジェクトの名称。
    /// </summary>
    public string ProjectName => _projectViewModel.Name;

    /// <summary>
    /// 表示対象となるタスクのリスト。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks;

    /// <summary>
    /// プロジェクトのマイルストーン。
    /// </summary>
    public ObservableCollection<Milestone> Milestones => _projectViewModel.Milestones;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projectViewModel">管理対象となるプロジェクトのViewModel。</param>
    /// <param name="userService">ユーザーサービス。</param>
    /// <param name="logger">ロガー。</param>
    public ProjectWorkspaceViewModel(ProjectViewModel projectViewModel, ICurrentUserService userService, ILogger<ProjectWorkspaceViewModel> logger)
    {
        _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        _logger = logger;

        // タスクリストの変更（再ロード等）を監視して、選択状態を維持する
        Tasks.CollectionChanged += OnTasksCollectionChanged;

        _logger.LogInformation("ProjectWorkspaceViewModel initialized for project {ProjectId}.", _projectViewModel.Id);
    }

    private Guid? _lastSelectedTaskId;

    partial void OnSelectedTaskChanged(ProjectTaskViewModel? value)
    {
        if (value != null)
        {
            _lastSelectedTaskId = value.Id;
        }
        AddCommentCommand.NotifyCanExecuteChanged();
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // リロード等によって選択が外れた場合、IDを元に再選択を試みる
        if (SelectedTask == null && _lastSelectedTaskId.HasValue)
        {
            var matchingTask = Tasks.FirstOrDefault(t => t.Id == _lastSelectedTaskId.Value);
            if (matchingTask != null)
            {
                _logger.LogDebug("Restoring selection for task {TaskId} after collection change.", _lastSelectedTaskId);
                SelectedTask = matchingTask;
            }
        }
    }

    /// <summary>
    /// 選択中のタスクにコメントを追加するコマンド。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddComment))]
    private void AddComment()
    {
        if (SelectedTask == null || string.IsNullOrWhiteSpace(NewCommentContent)) return;

        _logger.LogInformation("Adding comment to task {TaskId}.", SelectedTask.Id);

        try
        {
            var comment = new Comment
            {
                TaskId = SelectedTask.Id,
                AuthorId = _userService.GetCurrentUserId(),
                Content = NewCommentContent,
                CreatedAt = DateTime.Now
            };

            SelectedTask.Model.Comments.Add(comment);
            _logger.LogInformation("Comment added to task {TaskId}.", SelectedTask.Id);

            NewCommentContent = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add comment to task {TaskId}.", SelectedTask.Id);
        }
    }

    private bool CanAddComment() => SelectedTask != null && !string.IsNullOrWhiteSpace(NewCommentContent);

    [RelayCommand]
    private void ClearSelection() => SelectedTask = null;

    partial void OnNewCommentContentChanged(string value)
    {
        AddCommentCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// 新規マイルストーンを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddMilestone()
    {
        _logger.LogInformation("Attempting to add new milestone with label: {NewMilestoneLabel}", NewMilestoneLabel);
        if (string.IsNullOrWhiteSpace(NewMilestoneLabel))
        {
            _logger.LogWarning("Milestone label is empty. Cannot add milestone.");
            return;
        }

        try
        {
            _projectViewModel.Milestones.Add(new Milestone
            {
                Date = NewMilestoneDate,
                Label = NewMilestoneLabel
            });
            _logger.LogInformation("Milestone '{MilestoneLabel}' added to project {ProjectId}.", NewMilestoneLabel, _projectViewModel.Id);

            NewMilestoneLabel = string.Empty;
            NewMilestoneDate = DateTime.Today;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add milestone with label: {NewMilestoneLabel} to project {ProjectId}.", NewMilestoneLabel, _projectViewModel.Id);
        }
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
                ScheduledStartDate = NewTaskScheduledStartDate,
                Deadline = NewTaskDeadline,
                ActualStartDate = NewTaskActualStartDate,
                ActualEndDate = NewTaskActualEndDate,
                EstimatedCost = NewTaskEstimatedCost,
                ActualCost = NewTaskActualCost,
                Assignee = NewTaskAssignee
            };
            _projectViewModel.Model.Tasks.Add(taskEntity); // Modelのコレクションに追加
            _logger.LogInformation("Task '{TaskName}' (ID: {TaskId}) added to project {ProjectId}.", taskEntity.Name, taskEntity.Id, _projectViewModel.Id);

            NewTaskName = string.Empty;
            NewTaskDescription = string.Empty;
            NewTaskStatus = TimeLeaf.Models.Enums.TaskStatus.NotStarted;
            NewTaskPriority = TaskPriority.Medium;
            NewTaskScheduledStartDate = null;
            NewTaskDeadline = null;
            NewTaskActualStartDate = null;
            NewTaskActualEndDate = null;
            NewTaskEstimatedCost = 0;
            NewTaskActualCost = 0;
            NewTaskAssignee = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task with name: {NewTaskName} to project {ProjectId}.", NewTaskName, _projectViewModel.Id);
            // ここでUIにエラーを通知する等の処理を追加することも検討できます。
        }
    }
}
