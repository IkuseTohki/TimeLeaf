using System.Collections.ObjectModel;
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
    OverviewViewModel CreateOverviewViewModel(ObservableCollection<ProjectViewModel> projects);
    ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(ProjectViewModel projectViewModel);
    ProjectViewModel CreateProjectViewModel(Project project);
    AddProjectViewModel CreateAddProjectViewModel();
    AddTaskViewModel CreateAddTaskViewModel();

    ProjectDashboardViewModel CreateProjectDashboardViewModel(ProjectViewModel projectViewModel);
    ProjectTasksViewModel CreateProjectTasksViewModel(ProjectViewModel projectViewModel);
    ProjectTimelineViewModel CreateProjectTimelineViewModel(ProjectViewModel projectViewModel);
    ProjectSettingsViewModel CreateProjectSettingsViewModel(ProjectViewModel projectViewModel);
    ProjectNotificationsViewModel CreateProjectNotificationsViewModel();

    TaskSummaryViewModel CreateTaskSummaryViewModel(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel);
    TaskDetailViewModel CreateTaskDetailViewModel(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel);

    ApplicationSettingsViewModel CreateApplicationSettingsViewModel();
    ProfileEditViewModel CreateProfileEditViewModel();
}
