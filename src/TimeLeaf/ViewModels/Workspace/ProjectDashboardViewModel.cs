using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトの健康状態や概要を表示するダッシュボードのViewModel。
/// </summary>
public partial class ProjectDashboardViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IAddMilestoneUseCase _addMilestoneUseCase;
    private readonly IGetProjectUpcomingDeadlinesUseCase _getProjectUpcomingDeadlinesUseCase;
    private readonly IDialogService _dialogService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<ProjectDashboardViewModel> _logger;

    public string ProjectName => _projectViewModel.Name;
    public string Description => _projectViewModel.Description;
    public ObservableCollection<Milestone> Milestones => _projectViewModel.Milestones;
    public ObservableCollection<ProjectTask> UpcomingTasks { get; } = new();

    public int TotalTaskCount => _projectViewModel.TotalTaskCount;
    public int CompletedTaskCount => _projectViewModel.CompletedTaskCount;
    public double CompletionPercentage => _projectViewModel.CompletionPercentage;
    public double TotalEstimatedCost => _projectViewModel.TotalEstimatedCost;
    public double TotalActualCost => _projectViewModel.TotalActualCost;

    public bool IsAssignedToMe => _projectViewModel.IsAssignedToMe;

    public ProjectDashboardViewModel(
        ProjectViewModel projectViewModel,
        IAddMilestoneUseCase addMilestoneUseCase,
        IGetProjectUpcomingDeadlinesUseCase getProjectUpcomingDeadlinesUseCase,
        IDialogService dialogService,
        IViewModelFactory viewModelFactory,
        ILogger<ProjectDashboardViewModel> logger
    )
    {
        _projectViewModel = projectViewModel;
        _addMilestoneUseCase = addMilestoneUseCase;
        _getProjectUpcomingDeadlinesUseCase = getProjectUpcomingDeadlinesUseCase;
        _dialogService = dialogService;
        _viewModelFactory = viewModelFactory;
        _logger = logger;

        LoadUpcomingTasks();
    }

    private void LoadUpcomingTasks()
    {
        UpcomingTasks.Clear();
        var tasks = _getProjectUpcomingDeadlinesUseCase.Execute(_projectViewModel.Model, 3);
        foreach (var task in tasks)
        {
            UpcomingTasks.Add(task);
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddMilestone()
    {
        try
        {
            var addMilestoneVm = _viewModelFactory.CreateAddMilestoneViewModel();
            var result = await _dialogService.ShowDialogAsync(addMilestoneVm);

            if (result)
            {
                await _addMilestoneUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addMilestoneVm.Date,
                    addMilestoneVm.Label
                );
                _projectViewModel.SyncFromModel();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add milestone.");
        }
    }
}
