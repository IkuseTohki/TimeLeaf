using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクをグループ化して表示するためのコンテナ（親タスク）のViewModel。
/// </summary>
public partial class TaskContainerViewModel : ObservableObject
{
    /// <summary>
    /// 親タスクのViewModel。未分類の場合は null。
    /// </summary>
    [ObservableProperty]
    private ProjectTaskViewModel? _parentTask;

    /// <summary>
    /// 所属する子タスクのリスト。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> SubTasks { get; }

    /// <summary>
    /// 表示用の名称。
    /// </summary>
    public string DisplayName => ParentTask?.Name ?? "未分類のタスク";

    /// <summary>
    /// 未分類のコンテナかどうか。
    /// </summary>
    public bool IsUnclassified => ParentTask == null;

    /// <summary>
    /// タスクの総数。
    /// </summary>
    public int TotalTasksCount => SubTasks.Count;

    /// <summary>
    /// 完了したタスクの数。
    /// </summary>
    public int DoneTasksCount => SubTasks.Count(t => t.IsCompleted);

    /// <summary>
    /// このコンテナにタスクを追加するコマンド。
    /// </summary>
    public IRelayCommand AddTaskCommand { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="parentTask">親タスクのViewModel。</param>
    /// <param name="subTasks">子タスクのViewModelコレクション。</param>
    /// <param name="addTaskCommand">タスク追加コマンド。</param>
    public TaskContainerViewModel(
        ProjectTaskViewModel? parentTask,
        ObservableCollection<ProjectTaskViewModel> subTasks,
        IRelayCommand addTaskCommand
    )
    {
        _parentTask = parentTask;
        SubTasks = subTasks ?? throw new ArgumentNullException(nameof(subTasks));
        AddTaskCommand = addTaskCommand ?? throw new ArgumentNullException(nameof(addTaskCommand));
    }
}
