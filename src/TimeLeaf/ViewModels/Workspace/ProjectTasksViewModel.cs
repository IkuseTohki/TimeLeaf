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

    public ProjectTasksViewModel(
        ProjectViewModel projectViewModel,
        IAddTaskUseCase addTaskUseCase,
        IGetProjectMembersUseCase getProjectMembersUseCase,
        IViewModelFactory viewModelFactory,
        LeafKit.UI.Services.IDialogService dialogService,
        ILogger<ProjectTasksViewModel> logger,
        ILogger<TaskDetailViewModel> detailLogger
    )
    {
        _projectViewModel = projectViewModel;
        _addTaskUseCase = addTaskUseCase;
        _getProjectMembersUseCase = getProjectMembersUseCase;
        _viewModelFactory = viewModelFactory;
        _dialogService = dialogService;
        _logger = logger;
        _detailLogger = detailLogger;
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
        _logger.LogInformation("AddTask dialog started.");

        try
        {
            // プロジェクトのアサイン済みユーザーをロード
            var teammates = await _getProjectMembersUseCase.ExecuteAsync(_projectViewModel.Model);

            var addTaskVm = _viewModelFactory.CreateAddTaskViewModel(teammates);
            var result = await _dialogService.ShowDialogAsync(addTaskVm);

            if (result)
            {
                _logger.LogDebug("Adding task: {Name}", addTaskVm.Name);

                var assigneeName = addTaskVm.Assignee?.DisplayName ?? string.Empty;

                await _addTaskUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addTaskVm.Name,
                    addTaskVm.Description,
                    addTaskVm.Status,
                    addTaskVm.Priority,
                    null, // scheduledStartDate
                    addTaskVm.DueDate, // deadline
                    null, // actualStartDate
                    null, // actualEndDate
                    addTaskVm.EstimatedWorkHours ?? 0,
                    0, // actualCost
                    assigneeName
                );

                _projectViewModel.SyncFromModel();
                _logger.LogInformation("AddTask completed successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task.");
        }
    }
}
