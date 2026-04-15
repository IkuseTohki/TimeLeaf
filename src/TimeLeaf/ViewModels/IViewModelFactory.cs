using System;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ViewModel を生成するためのファクトリインターフェース。
/// プレゼンテーション層がフレームワーク（DIコンテナ等）に直接依存するのを防ぐ。
/// </summary>
public interface IViewModelFactory
{
    HomeViewModel CreateHomeViewModel(ObservableCollection<ProjectViewModel> projects);
    AllTasksViewModel CreateAllTasksViewModel(ObservableCollection<ProjectViewModel> projects);
    ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(
        ProjectViewModel projectViewModel,
        ObservableCollection<ProjectViewModel> projects
    );
    ProjectViewModel CreateProjectViewModel(Project project);
    ProjectTaskViewModel CreateProjectTaskViewModel(ProjectTask task);
    AddProjectViewModel CreateAddProjectViewModel();
    AddTaskViewModel CreateAddTaskViewModel(System.Collections.Generic.IEnumerable<User>? teammates = null);

    ProjectDashboardViewModel CreateProjectDashboardViewModel(ProjectViewModel projectViewModel);
    ProjectTasksViewModel CreateProjectTasksViewModel(ProjectViewModel projectViewModel);
    ProjectTimelineViewModel CreateProjectTimelineViewModel(ProjectViewModel projectViewModel);
    ProjectSettingsViewModel CreateProjectSettingsViewModel(ProjectViewModel projectViewModel);
    NotificationsViewModel CreateNotificationsViewModel(
        ObservableCollection<ProjectViewModel> projects,
        Guid? projectIdFilter = null
    );

    TaskSummaryViewModel CreateTaskSummaryViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel
    );
    TaskDetailViewModel CreateTaskDetailViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel
    );

    ApplicationSettingsViewModel CreateApplicationSettingsViewModel();
    ProfileEditViewModel CreateProfileEditViewModel();
}
