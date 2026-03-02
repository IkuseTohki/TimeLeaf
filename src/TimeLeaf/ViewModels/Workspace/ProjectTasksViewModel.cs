using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトのタスク一覧と操作を担当するViewModel。
/// </summary>
public partial class ProjectTasksViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IAddTaskUseCase _addTaskUseCase;
    private readonly IAddCommentUseCase _addCommentUseCase;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly LeafKit.UI.Services.IDialogService _dialogService;
    private readonly ILogger<ProjectTasksViewModel> _logger;
    private readonly ILogger<TaskDetailViewModel> _detailLogger;

    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks;

    [ObservableProperty]
    private ProjectTaskViewModel? _selectedTask;

    [ObservableProperty]
    private TaskDetailViewModel? _taskDetailViewModel;

    [ObservableProperty]
    private bool _isDetailVisible;

    public ProjectTasksViewModel(
        ProjectViewModel projectViewModel,
        IAddTaskUseCase addTaskUseCase,
        IAddCommentUseCase addCommentUseCase,
        IViewModelFactory viewModelFactory,
        LeafKit.UI.Services.IDialogService dialogService,
        ILogger<ProjectTasksViewModel> logger,
        ILogger<TaskDetailViewModel> detailLogger)
    {
        _projectViewModel = projectViewModel;
        _addTaskUseCase = addTaskUseCase;
        _addCommentUseCase = addCommentUseCase;
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
            TaskDetailViewModel = new TaskDetailViewModel(_projectViewModel, value, _addCommentUseCase, _detailLogger);
            IsDetailVisible = true;
        }
        else
        {
            IsDetailVisible = false;
            // メモリ解放を促すため、少し遅らせて null にすることを検討できるが、一旦即座に null
            TaskDetailViewModel = null;
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
    private async System.Threading.Tasks.Task AddTask()
    {
        _logger.LogInformation("AddTask dialog started.");

        try
        {
            var addTaskVm = _viewModelFactory.CreateAddTaskViewModel();
            var result = await _dialogService.ShowDialogAsync(addTaskVm);

            if (result)
            {
                _logger.LogDebug("Adding task: {Name}", addTaskVm.Name);

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
                    0,    // actualCost
                    addTaskVm.Assignee ?? string.Empty
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
