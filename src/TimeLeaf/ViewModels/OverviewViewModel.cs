using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトオーバービュー画面のロジックを担当するViewModel。
/// </summary>
public partial class OverviewViewModel : ObservableObject
{
    [ObservableProperty]
    private string _newProjectName = string.Empty;

    /// <summary>
    /// 表示対象となるプロジェクトのリスト。
    /// </summary>
    public ObservableCollection<Project> Projects { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projects">共有プロジェクトリスト。</param>
    public OverviewViewModel(ObservableCollection<Project> projects)
    {
        Projects = projects;
    }

    /// <summary>
    /// 新規プロジェクトを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddProject()
    {
        if (string.IsNullOrWhiteSpace(NewProjectName)) return;

        var project = new Project { Name = NewProjectName };
        Projects.Add(project);

        NewProjectName = string.Empty;
    }
}
