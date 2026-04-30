using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトのタスク一覧と操作を担当するViewModel。
/// </summary>
public partial class ProjectTasksViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IAddTaskUseCase _addTaskUseCase;
    private readonly IGetProjectMembersUseCase _getProjectMembersUseCase;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly LeafKit.UI.Services.IDialogService _dialogService;
    private readonly ILogger<ProjectTasksViewModel> _logger;
    private readonly ILogger<TaskDetailViewModel> _detailLogger;
    private readonly DetectProjectRisksUseCase _detectRisksUseCase;
    private readonly CalculateFlowLayoutUseCase _layoutUseCase;

    /// <summary>
    /// タスク詳細の表示がリクエストされたときに発生するイベント。
    /// </summary>
    public event EventHandler<ProjectTaskViewModel>? TaskDetailRequested;

    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks;

    [ObservableProperty]
    private ProjectTaskViewModel? _selectedTask;

    [ObservableProperty]
    private bool _isDetailVisible;

    [ObservableProperty]
    private ObservableCollection<ProjectRisk> _risks = new();

    [ObservableProperty]
    private string _weatherIcon = "☀️";

    [ObservableProperty]
    private string _weatherMessage = "順調です";

    [ObservableProperty]
    private ObservableCollection<TaskEdgeViewModel> _edges = new();

    public ObservableCollection<TaskContainerViewModel> TaskContainers { get; } = new();

    public ProjectTasksViewModel(
        ProjectViewModel projectViewModel,
        IAddTaskUseCase addTaskUseCase,
        IGetProjectMembersUseCase getProjectMembersUseCase,
        IViewModelFactory viewModelFactory,
        LeafKit.UI.Services.IDialogService dialogService,
        DetectProjectRisksUseCase detectRisksUseCase,
        CalculateFlowLayoutUseCase layoutUseCase,
        ILogger<ProjectTasksViewModel> logger,
        ILogger<TaskDetailViewModel> detailLogger
    )
    {
        _projectViewModel = projectViewModel;
        _addTaskUseCase = addTaskUseCase;
        _getProjectMembersUseCase = getProjectMembersUseCase;
        _viewModelFactory = viewModelFactory;
        _dialogService = dialogService;
        _detectRisksUseCase = detectRisksUseCase;
        _layoutUseCase = layoutUseCase;
        _logger = logger;
        _detailLogger = detailLogger;

        InitializeNodePositions();
        SyncEdges();
        RebuildContainers();
    }

    private void RebuildContainers()
    {
        TaskContainers.Clear();
        var allTasks = Tasks.ToList();

        // 1. 親タスク（コンテナ）となるタスクを抽出
        var parentIds = allTasks
            .Select(t => t.ParentId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToHashSet();
        var parents = allTasks.Where(t => parentIds.Contains(t.Id) || t.Model.Children.Any()).ToList();

        // 2. コンテナ作成
        foreach (var parent in parents)
        {
            var children = allTasks.Where(t => t.ParentId == parent.Id).ToList();
            if (children.Any() || allTasks.Contains(parent))
            {
                TaskContainers.Add(
                    new TaskContainerViewModel(
                        parent,
                        new ObservableCollection<ProjectTaskViewModel>(children),
                        AddTaskToContainerCommand
                    )
                );
            }
        }

        // 3. 未分類タスク
        var unclassified = allTasks
            .Where(t => !t.ParentId.HasValue && !parentIds.Contains(t.Id) && !t.Model.Children.Any())
            .ToList();
        if (unclassified.Any())
        {
            TaskContainers.Add(
                new TaskContainerViewModel(
                    null,
                    new ObservableCollection<ProjectTaskViewModel>(unclassified),
                    AddTaskToContainerCommand
                )
            );
        }
    }

    private void InitializeNodePositions()
    {
        var layout = _layoutUseCase.Execute(_projectViewModel.Model);

        foreach (var t in Tasks)
        {
            if (layout.TryGetValue(t.Id, out var pos))
            {
                t.X = pos.X;
                t.Y = pos.Y;
            }
        }
    }

    private void SyncEdges()
    {
        Edges.Clear();
        var vmMap = Tasks.ToDictionary(t => t.Id);

        foreach (var task in Tasks)
        {
            foreach (var constraint in task.Constraints)
            {
                if (vmMap.TryGetValue(constraint.PredecessorId, out var predecessor))
                {
                    Edges.Add(new TaskEdgeViewModel(predecessor, task));
                }
            }
        }
    }

    /// <summary>
    /// プロジェクトのリスクをスキャンして UI を更新します。
    /// </summary>
    public async System.Threading.Tasks.Task ScanRisksAsync()
    {
        try
        {
            var foundRisks = await _detectRisksUseCase.ExecuteAsync(_projectViewModel.Model);

            // UIスレッドでの更新（ObservableCollectionのため）
            Risks.Clear();
            foreach (var risk in foundRisks)
            {
                Risks.Add(risk);
            }

            // 各タスクViewModelのリスクフラグを更新
            var riskTaskIds = new HashSet<Guid>(Risks.Where(r => r.TaskId.HasValue).Select(r => r.TaskId!.Value));
            foreach (var task in Tasks)
            {
                task.HasRisk = riskTaskIds.Contains(task.Id);
            }

            // 天気アイコンの更新
            UpdateWeather();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan risks.");
        }
    }

    private void UpdateWeather()
    {
        if (Risks.Any(r => r.IsError))
        {
            WeatherIcon = "⚡";
            WeatherMessage = "深刻な問題が検出されました";
        }
        else if (Risks.Any())
        {
            WeatherIcon = "☁️";
            WeatherMessage = "リスクが予報されています";
        }
        else
        {
            WeatherIcon = "☀️";
            WeatherMessage = "順調に成長しています";
        }
    }

    partial void OnSelectedTaskChanged(ProjectTaskViewModel? value)
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = (task == value);
        }

        if (value != null)
        {
            IsDetailVisible = true;
        }
        else
        {
            IsDetailVisible = false;
        }
    }

    [RelayCommand]
    private void AddProject()
    {
        _ = AddTaskToContainer(null);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddTaskToContainer(TaskContainerViewModel? container)
    {
        try
        {
            var teammates = await _getProjectMembersUseCase.ExecuteAsync(_projectViewModel.Model);
            var addTaskVm = _viewModelFactory.CreateAddTaskViewModel(teammates);
            var result = await _dialogService.ShowDialogAsync(addTaskVm);

            if (result)
            {
                var assigneeName = addTaskVm.Assignee?.DisplayName ?? string.Empty;
                var parentId = container?.ParentTask?.Id;

                await _addTaskUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addTaskVm.Name,
                    addTaskVm.Description,
                    addTaskVm.Status,
                    addTaskVm.Priority,
                    parentId,
                    null, // scheduledStartDate
                    addTaskVm.DueDate,
                    null, // actualStartDate
                    null, // actualEndDate
                    addTaskVm.EstimatedWorkHours ?? 0,
                    0, // actualCost
                    assigneeName
                );

                _projectViewModel.SyncFromModel();
                RebuildContainers();
                await ScanRisksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddTask() => await AddTaskToContainer(null);

    [RelayCommand]
    private void OpenTaskDetailWindow(ProjectTaskViewModel task)
    {
        TriggerTaskDetailRequested(task);
    }

    public void TriggerTaskDetailRequested(ProjectTaskViewModel task)
    {
        _logger.LogInformation("Requesting task detail for: {TaskName}", task.Name);
        TaskDetailRequested?.Invoke(this, task);
    }
}
