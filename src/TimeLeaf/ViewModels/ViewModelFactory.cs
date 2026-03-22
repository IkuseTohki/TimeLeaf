using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels.Workspace;
using LeafKit.UI.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ViewModel を生成するための具体的なファクトリクラス。
/// </summary>
public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IIdentityService _identityService;

    public ViewModelFactory(IServiceProvider serviceProvider, IIdentityService identityService)
    {
        _serviceProvider = serviceProvider;
        _identityService = identityService;
    }

    public HomeViewModel CreateHomeViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new HomeViewModel(projects, new UserMenuViewModel(_identityService, _serviceProvider.GetRequiredService<IDialogService>()));
    }

    public AllTasksViewModel CreateAllTasksViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new AllTasksViewModel(projects);
    }

    public OverviewViewModel CreateOverviewViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new OverviewViewModel(
            projects,
            _serviceProvider.GetRequiredService<IAddProjectUseCase>(),
            _serviceProvider.GetRequiredService<IDialogService>(),
            this,
            _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());
    }

    public ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectWorkspaceViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<INotificationService>(),
            this,
            _serviceProvider.GetRequiredService<ICheckAssignmentUseCase>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>());
    }

    public ProjectViewModel CreateProjectViewModel(Project project)
    {
        return new ProjectViewModel(
            project,
            _identityService.CurrentUserId,
            _serviceProvider.GetRequiredService<IJoinProjectUseCase>());
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
            this,
            _serviceProvider.GetRequiredService<IDialogService>(),
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

    public ProjectNotificationsViewModel CreateProjectNotificationsViewModel()
    {
        return new ProjectNotificationsViewModel(
            _serviceProvider.GetRequiredService<INotificationService>());
    }

    public TaskSummaryViewModel CreateTaskSummaryViewModel(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel)
    {
        return new TaskSummaryViewModel(projectViewModel, taskViewModel);
    }

    public TaskDetailViewModel CreateTaskDetailViewModel(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel)
    {
        return new TaskDetailViewModel(
            projectViewModel,
            taskViewModel,
            _serviceProvider.GetRequiredService<IAddCommentUseCase>(),
            _serviceProvider.GetRequiredService<ILogger<TaskDetailViewModel>>());
    }
}
