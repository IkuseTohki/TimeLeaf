using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ProjectTaskエンティティをラップし、UIバインディングのための通知機能を提供するViewModel。
/// </summary>
public partial class ProjectTaskViewModel : ObservableObject
{
    private ProjectTask _projectTask;

    /// <summary>
    /// 基になるProjectTaskエンティティ。
    /// </summary>
    public ProjectTask Model => _projectTask;

    public Guid Id => _projectTask.Id;

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

    public TaskStatus Status
    {
        get => _projectTask.Status;
        set
        {
            if (_projectTask.Status != value)
            {
                _projectTask.UpdateStatus(value);
                OnPropertyChanged(nameof(Status));
                // ステータス変更により開始・終了日が自動設定される可能性があるため通知
                OnPropertyChanged(nameof(ActualStartDate));
                OnPropertyChanged(nameof(ActualEndDate));
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
                _projectTask.UpdateSchedule(value?.ToUniversalTime(), _projectTask.Deadline);
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
                _projectTask.UpdateSchedule(_projectTask.ScheduledStartDate, value?.ToUniversalTime());
                OnPropertyChanged(nameof(Deadline));
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
                _projectTask.UpdateActualDates(value?.ToUniversalTime(), _projectTask.ActualEndDate);
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
                _projectTask.UpdateActualDates(_projectTask.ActualStartDate, value?.ToUniversalTime());
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

    public string Assignee
    {
        get => _projectTask.Assignee;
        set
        {
            if (_projectTask.Assignee != value)
            {
                _projectTask.AssignTo(value);
                OnPropertyChanged(nameof(Assignee));
            }
        }
    }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>
    /// 依存タスクのIDリスト。
    /// </summary>
    public System.Collections.Generic.List<Guid> Dependencies => _projectTask.Dependencies;

    /// <summary>
    /// タスクに関するコメントのリスト（UI用）。
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<Comment> Comments { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projectTask">ラップするProjectTaskエンティティ。</param>
    public ProjectTaskViewModel(ProjectTask projectTask)
    {
        _projectTask = projectTask ?? throw new ArgumentNullException(nameof(projectTask));
        SyncComments();
    }

    private void SyncComments()
    {
        // レコード（値）の不一致がある場合のみ更新
        if (!Comments.SequenceEqual(_projectTask.Comments))
        {
            Comments.Clear();
            foreach (var comment in _projectTask.Comments)
            {
                Comments.Add(comment);
            }
        }
    }

    /// <summary>
    /// モデルの状態を最新のエンティティで更新し、通知を発生させます。
    /// </summary>
    /// <param name="newModel">最新の状態を持つエンティティ。</param>
    public void UpdateFromModel(ProjectTask newModel)
    {
        if (newModel == null) throw new ArgumentNullException(nameof(newModel));
        if (newModel.Id != _projectTask.Id) throw new ArgumentException("Cannot update ViewModel with a different Task ID.");

        _projectTask = newModel;
        SyncComments();

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
        OnPropertyChanged(nameof(Comments));
    }
}
