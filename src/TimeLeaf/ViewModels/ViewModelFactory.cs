using System;
using System.Collections.ObjectModel;
using LeafKit.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories;
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
    private readonly IIdentityService _identityService;
    private readonly IUserService _userService;

    public ViewModelFactory(
        IServiceProvider serviceProvider,
        IIdentityService identityService,
        IUserService userService
    )
    {
        _serviceProvider = serviceProvider;
        _identityService = identityService;
        _userService = userService;
    }

    public HomeViewModel CreateHomeViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new HomeViewModel(
            projects,
            new UserMenuViewModel(
                _identityService,
                _userService,
                _serviceProvider.GetRequiredService<IDialogService>(),
                this
            )
        );
    }

    public AllTasksViewModel CreateAllTasksViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        return new AllTasksViewModel(projects, _identityService);
    }

    public ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(
        ProjectViewModel projectViewModel,
        ObservableCollection<ProjectViewModel> projects
    )
    {
        return new ProjectWorkspaceViewModel(
            projectViewModel,
            projects,
            _serviceProvider.GetRequiredService<INotificationService>(),
            this,
            _serviceProvider.GetRequiredService<ICheckAssignmentUseCase>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>()
        );
    }

    public ProjectViewModel CreateProjectViewModel(Project project)
    {
        return new ProjectViewModel(
            project,
            _identityService.CurrentUserId,
            _serviceProvider.GetRequiredService<IJoinProjectUseCase>(),
            this
        );
    }

    public ProjectTaskViewModel CreateProjectTaskViewModel(ProjectTask task)
    {
        return new ProjectTaskViewModel(task, _userService);
    }

    public AddProjectViewModel CreateAddProjectViewModel()
    {
        return _serviceProvider.GetRequiredService<AddProjectViewModel>();
    }

    public AddTaskViewModel CreateAddTaskViewModel(System.Collections.Generic.IEnumerable<User>? teammates = null)
    {
        var vm = _serviceProvider.GetRequiredService<AddTaskViewModel>();
        if (teammates != null)
        {
            vm.Teammates = new ObservableCollection<User>(teammates);
        }
        return vm;
    }

    public AddMilestoneViewModel CreateAddMilestoneViewModel()
    {
        return _serviceProvider.GetRequiredService<AddMilestoneViewModel>();
    }

    public AddContainerViewModel CreateAddContainerViewModel()
    {
        return _serviceProvider.GetRequiredService<AddContainerViewModel>();
    }

    public ReorderProjectItemsViewModel CreateReorderProjectItemsViewModel(IWorkItemContainer rootContainer)
    {
        return new ReorderProjectItemsViewModel(rootContainer);
    }

    public ProjectDashboardViewModel CreateProjectDashboardViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectDashboardViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<IAddMilestoneUseCase>(),
            _serviceProvider.GetRequiredService<IGetProjectUpcomingDeadlinesUseCase>(),
            _serviceProvider.GetRequiredService<IDialogService>(),
            this,
            _serviceProvider.GetRequiredService<ILogger<ProjectDashboardViewModel>>()
        );
    }

    public ProjectTasksViewModel CreateProjectTasksViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectTasksViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<IAddTaskUseCase>(),
            _serviceProvider.GetRequiredService<IAddContainerUseCase>(),
            _serviceProvider.GetRequiredService<IMoveTaskUseCase>(),
            _serviceProvider.GetRequiredService<IDeleteTaskUseCase>(),
            _serviceProvider.GetRequiredService<IDeleteContainerUseCase>(),
            _serviceProvider.GetRequiredService<IGetProjectMembersUseCase>(),
            this,
            _serviceProvider.GetRequiredService<IDialogService>(),
            _serviceProvider.GetRequiredService<DetectProjectRisksUseCase>(),
            _serviceProvider.GetRequiredService<CalculateFlowLayoutUseCase>(),
            _serviceProvider.GetRequiredService<ILogger<ProjectTasksViewModel>>(),
            _serviceProvider.GetRequiredService<ILogger<TaskDetailViewModel>>()
        );
    }

    public ProjectTimelineViewModel CreateProjectTimelineViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectTimelineViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<GetTimelineRowsUseCase>(),
            _serviceProvider.GetRequiredService<WorkdayService>()
        );
    }

    public ProjectFlowViewModel CreateProjectFlowViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectFlowViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<CalculateFlowLayoutUseCase>(),
            _userService
        );
    }

    public ProjectSettingsViewModel CreateProjectSettingsViewModel(ProjectViewModel projectViewModel)
    {
        return new ProjectSettingsViewModel(
            projectViewModel,
            _serviceProvider.GetRequiredService<IProjectRepository>(),
            _identityService,
            _serviceProvider.GetRequiredService<INotificationService>(),
            _serviceProvider.GetRequiredService<IDialogService>()
        );
    }

    public NotificationsViewModel CreateNotificationsViewModel(
        ObservableCollection<ProjectViewModel> projects,
        Guid? projectIdFilter = null
    )
    {
        return new NotificationsViewModel(
            _serviceProvider.GetRequiredService<INotificationService>(),
            projects,
            _serviceProvider.GetRequiredService<ILogger<NotificationsViewModel>>(),
            projectIdFilter
        );
    }

    public TaskDetailViewModel CreateTaskDetailViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel
    )
    {
        return new TaskDetailViewModel(
            projectViewModel,
            taskViewModel,
            _serviceProvider.GetRequiredService<IAddCommentUseCase>(),
            _serviceProvider.GetRequiredService<ISaveProjectUseCase>(),
            _serviceProvider.GetRequiredService<IDeleteTaskUseCase>(),
            _serviceProvider.GetRequiredService<IDialogService>(),
            _serviceProvider.GetRequiredService<IUserService>(),
            _serviceProvider.GetRequiredService<IProjectService>(),
            _serviceProvider.GetRequiredService<ILogger<TaskDetailViewModel>>(),
            _serviceProvider.GetRequiredService<IGetProjectMembersUseCase>(),
            _serviceProvider.GetRequiredService<IGetTaskHistoryUseCase>()
        );
    }

    public ApplicationSettingsViewModel CreateApplicationSettingsViewModel()
    {
        return _serviceProvider.GetRequiredService<ApplicationSettingsViewModel>();
    }

    public ProfileEditViewModel CreateProfileEditViewModel()
    {
        return _serviceProvider.GetRequiredService<ProfileEditViewModel>();
    }

    public UserManagementViewModel CreateUserManagementViewModel()
    {
        return new UserManagementViewModel(
            _userService,
            _identityService,
            _serviceProvider.GetRequiredService<IDialogService>()
        );
    }
}
