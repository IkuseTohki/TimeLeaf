using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Services;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ProjectTaskエンティティをラップし、UIバインディングのための通知機能を提供するViewModel。
/// </summary>
public partial class ProjectTaskViewModel : ObservableObject, IDisposable
{
    private ProjectTask _projectTask;
    private readonly IUserService _userService;

    /// <summary>
    /// 詳細画面への遷移を要求するイベント。
    /// </summary>
    public static event EventHandler<ProjectTaskViewModel>? GlobalRequestDetail;

    [RelayCommand]
    public void RequestDetail()
    {
        GlobalRequestDetail?.Invoke(this, this);
    }

    [ObservableProperty]
    private string _projectName = string.Empty;

    /// <summary>
    /// 基になるProjectTaskエンティティ。
    /// </summary>
    public ProjectTask Model => _projectTask;

    public Guid Id => _projectTask.Id;

    public Guid? ParentId => _projectTask.ParentId;

    public string Name
    {
        get => _projectTask.Name;
        set
        {
            if (_projectTask.Name != value)
            {
                _projectTask.UpdateName(value);
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public string Description
    {
        get => _projectTask.Description;
        set
        {
            if (_projectTask.Description != value)
            {
                _projectTask.UpdateDescription(value);
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public TimeLeaf.Models.Enums.TaskStatus Status
    {
        get => _projectTask.Status;
        set
        {
            if (_projectTask.Status != value)
            {
                _projectTask.UpdateStatus(value);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ActualStartDate));
                OnPropertyChanged(nameof(ActualEndDate));
                OnPropertyChanged(nameof(IsCompleted));
            }
        }
    }

    public TaskPriority Priority
    {
        get => _projectTask.Priority;
        set
        {
            if (_projectTask.Priority != value)
            {
                _projectTask.UpdatePriority(value);
                OnPropertyChanged(nameof(Priority));
            }
        }
    }

    public DateTime? ScheduledStartDate
    {
        get => _projectTask.ScheduledStartDate;
        set
        {
            if (_projectTask.ScheduledStartDate != value)
            {
                _projectTask.UpdateSchedule(value, _projectTask.Deadline);
                OnPropertyChanged(nameof(ScheduledStartDate));
            }
        }
    }

    public DateTime? Deadline
    {
        get => _projectTask.Deadline;
        set
        {
            if (_projectTask.Deadline != value)
            {
                _projectTask.UpdateSchedule(_projectTask.ScheduledStartDate, value);
                OnPropertyChanged(nameof(Deadline));
                OnPropertyChanged(nameof(DeadlineGroup));
            }
        }
    }

    public DateTime? ActualStartDate
    {
        get => _projectTask.ActualStartDate;
        set
        {
            if (_projectTask.ActualStartDate != value)
            {
                _projectTask.UpdateActualDates(value, _projectTask.ActualEndDate);
                OnPropertyChanged(nameof(ActualStartDate));
            }
        }
    }

    public DateTime? ActualEndDate
    {
        get => _projectTask.ActualEndDate;
        set
        {
            if (_projectTask.ActualEndDate != value)
            {
                _projectTask.UpdateActualDates(_projectTask.ActualStartDate, value);
                OnPropertyChanged(nameof(ActualEndDate));
            }
        }
    }

    public double EstimatedCost
    {
        get => _projectTask.EstimatedCost;
        set
        {
            if (Math.Abs(_projectTask.EstimatedCost - value) > double.Epsilon)
            {
                _projectTask.UpdateEstimatedCost(value);
                OnPropertyChanged(nameof(EstimatedCost));
            }
        }
    }

    public double ActualCost
    {
        get => _projectTask.ActualCost;
        set
        {
            if (Math.Abs(_projectTask.ActualCost - value) > double.Epsilon)
            {
                _projectTask.UpdateActualCost(value);
                OnPropertyChanged(nameof(ActualCost));
            }
        }
    }

    public Guid? Assignee
    {
        get => _projectTask.Assignee;
        set
        {
            if (_projectTask.Assignee != value)
            {
                _projectTask.AssignTo(value);
                OnPropertyChanged(nameof(Assignee));
                OnPropertyChanged(nameof(AssigneeName));
                _ = UpdateAssigneePropertiesAsync();
            }
        }
    }

    /// <summary>
    /// 担当者の表示名。
    /// </summary>
    public string AssigneeName
    {
        get => _userService.GetUserName(Assignee?.ToString() ?? string.Empty);
        set
        {
            var userIdStr = _userService.GetUserIdByName(value);
            if (Guid.TryParse(userIdStr, out var userId))
            {
                Assignee = userId;
            }
            else
            {
                Assignee = null;
            }
        }
    }

    [ObservableProperty]
    private string _assigneeInitial = "?";

    [ObservableProperty]
    private string _assigneeColor = "#72796e";

    private System.Threading.CancellationTokenSource? _assigneeUpdateCts;

    private async System.Threading.Tasks.Task UpdateAssigneePropertiesAsync()
    {
        _assigneeUpdateCts?.Cancel();
        _assigneeUpdateCts = new System.Threading.CancellationTokenSource();
        var ct = _assigneeUpdateCts.Token;

        try
        {
            if (Assignee.HasValue)
            {
                var user = await _userService.GetUserAsync(Assignee.Value);
                if (ct.IsCancellationRequested)
                    return;

                if (user != null)
                {
                    AssigneeInitial = string.IsNullOrEmpty(user.DisplayName) ? "?" : user.DisplayName.Substring(0, 1);
                    AssigneeColor = user.ThemeColor;
                    return;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            // ロギングは必要だが、ここではデフォルト値に戻す
            System.Diagnostics.Debug.WriteLine($"Failed to update assignee properties: {ex.Message}");
        }

        if (!ct.IsCancellationRequested)
        {
            AssigneeInitial = "?";
            AssigneeColor = "#72796e";
        }
    }

    /// <summary>
    /// タスクが完了状態かどうか。
    /// </summary>
    public bool IsCompleted => Status == TimeLeaf.Models.Enums.TaskStatus.Completed;

    /// <summary>
    /// タイムライン表示用のグループ名。
    /// </summary>
    public string DeadlineGroup
    {
        get
        {
            if (!Deadline.HasValue)
                return "Future / Someday";
            var date = Deadline.Value.Date;
            var today = DateTime.Today;
            if (date == today)
                return "Today";
            if (date == today.AddDays(1))
                return "Tomorrow";
            if (date <= today.AddDays(7))
                return "This Week";
            return "Later";
        }
    }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private bool _hasRisk;

    public IReadOnlyList<TaskConstraint> Constraints => _projectTask.Constraints;

    public IReadOnlyList<Guid> Dependencies => _projectTask.Constraints.Select(c => c.PredecessorId).ToList();

    /// <summary>
    /// 優先度の全選択肢。
    /// </summary>
    public IEnumerable<TaskPriority> PriorityValues => Enum.GetValues(typeof(TaskPriority)).Cast<TaskPriority>();

    /// <summary>
    /// ステータスの全選択肢。
    /// </summary>
    public IEnumerable<TimeLeaf.Models.Enums.TaskStatus> StatusValues =>
        Enum.GetValues(typeof(TimeLeaf.Models.Enums.TaskStatus)).Cast<TimeLeaf.Models.Enums.TaskStatus>();

    /// <summary>
    /// タスクに関するコメントのリスト（UI用）。
    /// </summary>
    public ObservableCollection<CommentViewModel> Comments { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public ProjectTaskViewModel(ProjectTask projectTask, IUserService userService)
    {
        _projectTask = projectTask ?? throw new ArgumentNullException(nameof(projectTask));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        SyncComments();
        _ = UpdateAssigneePropertiesAsync();
    }

    private void SyncComments()
    {
        // エンティティ側のコメントと同期（簡易的な実装）
        if (Comments.Count != _projectTask.Comments.Count)
        {
            // 既存の ViewModel を破棄（イベント購読解除のため）
            foreach (var cvm in Comments)
            {
                cvm.Dispose();
            }
            Comments.Clear();

            foreach (var comment in _projectTask.Comments)
            {
                var commentVm = new CommentViewModel(comment, _userService);
                Comments.Add(commentVm);
            }
        }
    }

    /// <summary>
    /// モデルの状態を最新のエンティティで更新し、通知を発生させます。
    /// </summary>
    public void UpdateFromModel(ProjectTask newModel)
    {
        if (newModel == null)
            throw new ArgumentNullException(nameof(newModel));
        if (newModel.Id != _projectTask.Id)
            throw new ArgumentException("Cannot update ViewModel with a different Task ID.");

        _projectTask = newModel;
        SyncComments();
        _ = UpdateAssigneePropertiesAsync();

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Priority));
        OnPropertyChanged(nameof(ScheduledStartDate));
        OnPropertyChanged(nameof(Deadline));
        OnPropertyChanged(nameof(ActualStartDate));
        OnPropertyChanged(nameof(ActualEndDate));
        OnPropertyChanged(nameof(EstimatedCost));
        OnPropertyChanged(nameof(ActualCost));
        OnPropertyChanged(nameof(Assignee));
        OnPropertyChanged(nameof(Dependencies));
        OnPropertyChanged(nameof(Constraints));
        OnPropertyChanged(nameof(HasRisk));
        OnPropertyChanged(nameof(Comments));
        OnPropertyChanged(nameof(DeadlineGroup));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(AssigneeName));
    }

    public void Dispose()
    {
        foreach (var cvm in Comments)
        {
            cvm.Dispose();
        }
        Comments.Clear();
    }
}
