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
    OverviewViewModel CreateOverviewViewModel(ObservableCollection<ProjectViewModel> projects);
    ProjectWorkspaceViewModel CreateProjectWorkspaceViewModel(ProjectViewModel projectViewModel);
    AddProjectViewModel CreateAddProjectViewModel();
    AddTaskViewModel CreateAddTaskViewModel();

    ProjectDashboardViewModel CreateProjectDashboardViewModel(ProjectViewModel projectViewModel);
    ProjectTasksViewModel CreateProjectTasksViewModel(ProjectViewModel projectViewModel);
    ProjectTimelineViewModel CreateProjectTimelineViewModel(ProjectViewModel projectViewModel);
    ProjectSettingsViewModel CreateProjectSettingsViewModel(ProjectViewModel projectViewModel);
}
