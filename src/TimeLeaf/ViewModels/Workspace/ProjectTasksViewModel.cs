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

    /// <summary>
    /// タスク詳細の表示がリクエストされたときに発生するイベント。
    /// </summary>
    public event EventHandler<ProjectTaskViewModel>? TaskDetailRequested;

    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks;

    [ObservableProperty]
    private ProjectTaskViewModel? _selectedTask;

    [ObservableProperty]
    private TaskSummaryViewModel? _taskSummaryViewModel;

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

    public ProjectTasksViewModel(
        ProjectViewModel projectViewModel,
        IAddTaskUseCase addTaskUseCase,
        IGetProjectMembersUseCase getProjectMembersUseCase,
        IViewModelFactory viewModelFactory,
        LeafKit.UI.Services.IDialogService dialogService,
        DetectProjectRisksUseCase detectRisksUseCase,
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
        _logger = logger;
        _detailLogger = detailLogger;
    }

    private void InitializeNodePositions()
    {
        // 簡易的な初期配置ロジック（グリッド状）
        double startX = 50;
        double startY = 50;
        double offsetX = 200;
        double offsetY = 100;
        int cols = 4;

        for (int i = 0; i < Tasks.Count; i++)
        {
            var t = Tasks[i];
            // すでに座標がある場合は維持（将来的に保存された座標を使う）
            if (t.X == 0 && t.Y == 0)
            {
                t.X = startX + (i % cols) * offsetX;
                t.Y = startY + (i / cols) * offsetY;
            }
        }
    }

    private void SyncEdges()
    {
        Edges.Clear();
        foreach (var task in Tasks)
        {
            foreach (var constraint in task.Constraints)
            {
                var predecessor = Tasks.FirstOrDefault(t => t.Id == constraint.PredecessorId);
                if (predecessor != null)
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
            TaskSummaryViewModel = _viewModelFactory.CreateTaskSummaryViewModel(_projectViewModel, value);
            IsDetailVisible = true;
        }
        else
        {
            IsDetailVisible = false;
            TaskSummaryViewModel = null;
        }
    }

    [RelayCommand]
    private void CloseDetail() => SelectedTask = null;

    [RelayCommand]
    private void SelectTask(ProjectTaskViewModel task)
    {
        SelectedTask = task;
    }

    [RelayCommand]
    private void OpenTaskDetailWindow(ProjectTaskViewModel task)
    {
        _logger.LogInformation("Requesting task detail for: {TaskName}", task.Name);
        TaskDetailRequested?.Invoke(this, task);
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddTask()
    {
        try
        {
            var teammates = await _getProjectMembersUseCase.ExecuteAsync(_projectViewModel.Model);
            var addTaskVm = _viewModelFactory.CreateAddTaskViewModel(teammates);
            var result = await _dialogService.ShowDialogAsync(addTaskVm);

            if (result)
            {
                var assigneeName = addTaskVm.Assignee?.DisplayName ?? string.Empty;

                await _addTaskUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addTaskVm.Name,
                    addTaskVm.Description,
                    addTaskVm.Status,
                    addTaskVm.Priority,
                    null,
                    addTaskVm.DueDate,
                    null,
                    null,
                    addTaskVm.EstimatedWorkHours ?? 0,
                    0,
                    assigneeName
                );

                _projectViewModel.SyncFromModel();
                await ScanRisksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task.");
        }
    }
}
