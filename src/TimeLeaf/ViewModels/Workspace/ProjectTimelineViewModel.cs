using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトのスケジュール（ガントチャート）を表示するViewModel。
/// </summary>
public partial class ProjectTimelineViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;

    public ProjectTimelineViewModel(ProjectViewModel projectViewModel)
    {
        _projectViewModel = projectViewModel;
    }
}
