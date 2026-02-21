using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のタスク管理（ワークスペース）を担当するViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly Project _project;

    [ObservableProperty]
    private string _newTaskName = string.Empty;

    /// <summary>
    /// 表示対象となるタスクのリスト。
    /// </summary>
    public ObservableCollection<Task> Tasks { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="project">管理対象となるプロジェクト。</param>
    public ProjectWorkspaceViewModel(Project project)
    {
        _project = project;
        // 初期タスクがある場合はリストに反映する
        foreach (var task in _project.Tasks)
        {
            Tasks.Add(task);
        }
    }

    /// <summary>
    /// 新規タスクを追加するコマンド。
    /// </summary>
    [RelayCommand]
    private void AddTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskName)) return;

        var task = new Task { Name = NewTaskName };
        _project.Tasks.Add(task);
        Tasks.Add(task);

        NewTaskName = string.Empty;
    }
}
