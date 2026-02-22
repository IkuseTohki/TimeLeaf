using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ProjectTaskエンティティをラップし、UIバインディングのための通知機能を提供するViewModel。
/// </summary>
public partial class ProjectTaskViewModel : ObservableObject
{
    private readonly ProjectTask _projectTask;

    /// <summary>
    /// 基になるProjectTaskエンティティ。
    /// </summary>
    public ProjectTask Model => _projectTask;

    public Guid Id => _projectTask.Id;

    public string Name
    {
        get => _projectTask.Name;
        set => SetProperty(_projectTask.Name, value, _projectTask, (model, val) => model.Name = val);
    }

    public string Description
    {
        get => _projectTask.Description;
        set => SetProperty(_projectTask.Description, value, _projectTask, (model, val) => model.Description = val);
    }

    public TaskStatus Status
    {
        get => _projectTask.Status;
        set => SetProperty(_projectTask.Status, value, _projectTask, (model, val) => model.Status = val);
    }

    public TaskPriority Priority
    {
        get => _projectTask.Priority;
        set => SetProperty(_projectTask.Priority, value, _projectTask, (model, val) => model.Priority = val);
    }

    public DateTime? Deadline
    {
        get => _projectTask.Deadline;
        set => SetProperty(_projectTask.Deadline, value, _projectTask, (model, val) => model.Deadline = val);
    }

    public double EstimatedCost
    {
        get => _projectTask.EstimatedCost;
        set => SetProperty(_projectTask.EstimatedCost, value, _projectTask, (model, val) => model.EstimatedCost = val);
    }

    public double ActualCost
    {
        get => _projectTask.ActualCost;
        set => SetProperty(_projectTask.ActualCost, value, _projectTask, (model, val) => model.ActualCost = val);
    }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projectTask">ラップするProjectTaskエンティティ。</param>
    public ProjectTaskViewModel(ProjectTask projectTask)
    {
        _projectTask = projectTask ?? throw new ArgumentNullException(nameof(projectTask));
    }
}
