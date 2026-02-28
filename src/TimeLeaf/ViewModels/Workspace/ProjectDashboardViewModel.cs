using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private readonly ILogger<ProjectDashboardViewModel> _logger;

    public string ProjectName { get => _projectViewModel.Name; set { } }
    public string Description { get => _projectViewModel.Description; set { } }
    public ObservableCollection<Milestone> Milestones => _projectViewModel.Milestones;

    public int TotalTaskCount { get => _projectViewModel.TotalTaskCount; set { } }
    public int CompletedTaskCount { get => _projectViewModel.CompletedTaskCount; set { } }
    public double CompletionPercentage { get => _projectViewModel.CompletionPercentage; set { } }
    public double TotalEstimatedCost { get => _projectViewModel.TotalEstimatedCost; set { } }
    public double TotalActualCost { get => _projectViewModel.TotalActualCost; set { } }

    [ObservableProperty]
    private DateTime _newMilestoneDate = DateTime.Today;

    [ObservableProperty]
    private string _newMilestoneLabel = string.Empty;

    public ProjectDashboardViewModel(
        ProjectViewModel projectViewModel,
        IAddMilestoneUseCase addMilestoneUseCase,
        ILogger<ProjectDashboardViewModel> logger)
    {
        _projectViewModel = projectViewModel;
        _addMilestoneUseCase = addMilestoneUseCase;
        _logger = logger;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddMilestone()
    {
        if (string.IsNullOrWhiteSpace(NewMilestoneLabel)) return;

        try
        {
            await _addMilestoneUseCase.ExecuteAsync(_projectViewModel.Model, NewMilestoneDate, NewMilestoneLabel);
            _projectViewModel.SyncFromModel();

            NewMilestoneLabel = string.Empty;
            NewMilestoneDate = DateTime.Today;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add milestone.");
        }
    }
}
