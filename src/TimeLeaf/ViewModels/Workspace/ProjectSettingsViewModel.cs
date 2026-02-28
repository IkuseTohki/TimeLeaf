using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクトの設定（名称変更、アーカイブ等）を担当するViewModel。
/// </summary>
public partial class ProjectSettingsViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;

    public ProjectSettingsViewModel(ProjectViewModel projectViewModel)
    {
        _projectViewModel = projectViewModel;
    }
}
