using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ViewModel を生成するための具体的なファクトリクラス。
/// </summary>
public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ViewModelFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public OverviewViewModel CreateOverviewViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new OverviewViewModel(
            projects,
            _serviceProvider.GetRequiredService<IAddProjectUseCase>(),
            _serviceProvider.GetRequiredService<LeafKit.UI.Services.IDialogService>(),
            this,
            _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());
    }

    public ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectWorkspaceViewModel(
            projectViewModel,
            this,
            _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>());
    }

    public AddProjectViewModel CreateAddProjectViewModel()
    {
        return _serviceProvider.GetRequiredService<AddProjectViewModel>();
    }

    public AddTaskViewModel CreateAddTaskViewModel()
    {
        return _serviceProvider.GetRequiredService<AddTaskViewModel>();
    }

    public ProjectDashboardViewModel CreateProjectDashboardViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectDashboardViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<IAddMilestoneUseCase>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectDashboardViewModel>>());
    }

    public ProjectTasksViewModel CreateProjectTasksViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectTasksViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<IAddTaskUseCase>(),
            _serviceProvider.GetRequiredService<IAddCommentUseCase>(),
            this,
            _serviceProvider.GetRequiredService<LeafKit.UI.Services.IDialogService>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectTasksViewModel>>(),
            _serviceProvider.GetRequiredService<ILogger<TaskDetailViewModel>>());
    }

    public ProjectTimelineViewModel CreateProjectTimelineViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectTimelineViewModel(projectViewModel);
    }

    public ProjectSettingsViewModel CreateProjectSettingsViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectSettingsViewModel(projectViewModel);
    }
}
