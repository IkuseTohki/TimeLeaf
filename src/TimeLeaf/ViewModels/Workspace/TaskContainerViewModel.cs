using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクをグループ化して表示するためのコンテナのViewModel。
/// ProjectContainer エンティティをラップし、UIに必要な情報を公開する。
/// </summary>
public partial class TaskContainerViewModel : ObservableObject
{
    private readonly ProjectContainer? _container;

    /// <summary>
    /// 所属する子タスクのリスト。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> SubTasks { get; }

    /// <summary>
    /// 表示用の名称。
    /// </summary>
    public string DisplayName => _container?.Name ?? "未分類のタスク";

    /// <summary>
    /// 未分類のコンテナかどうか。
    /// </summary>
    public bool IsUnclassified => _container == null;

    /// <summary>
    /// 内包するエンティティのID（未分類の場合は Empty）。
    /// </summary>
    public Guid Id => _container?.Id ?? Guid.Empty;

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
    /// このコンテナを削除するコマンド。
    /// </summary>
    public IRelayCommand DeleteContainerCommand { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="container">ラップ対象のコンテナエンティティ。未分類の場合は null。</param>
    /// <param name="subTasks">このコンテナに属するタスクのコレクション。</param>
    /// <param name="addTaskCommand">タスク追加コマンド。</param>
    /// <param name="deleteContainerCommand">コンテナ削除コマンド。</param>
    public TaskContainerViewModel(
        ProjectContainer? container,
        ObservableCollection<ProjectTaskViewModel> subTasks,
        IRelayCommand addTaskCommand,
        IRelayCommand deleteContainerCommand
    )
    {
        _container = container;
        SubTasks = subTasks ?? throw new ArgumentNullException(nameof(subTasks));
        AddTaskCommand = addTaskCommand ?? throw new ArgumentNullException(nameof(addTaskCommand));
        DeleteContainerCommand =
            deleteContainerCommand ?? throw new ArgumentNullException(nameof(deleteContainerCommand));
    }
}
