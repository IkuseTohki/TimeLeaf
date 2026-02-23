using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.ViewModels;

/// <summary>
/// Projectエンティティをラップし、UIバインディングのための通知機能を提供するViewModel。
/// </summary>
public partial class ProjectViewModel : ObservableObject
{
    private readonly Project _project;

    /// <summary>
    /// 基になるProjectエンティティ。
    /// </summary>
    public Project Model => _project;

    public Guid Id => _project.Id;

    public string Name
    {
        get => _project.Name;
        set => SetProperty(_project.Name, value, _project, (model, val) => model.Name = val);
    }

    public string Description
    {
        get => _project.Description;
        set => SetProperty(_project.Description, value, _project, (model, val) => model.Description = val);
    }

    public TimeLeaf.Models.Enums.ProjectStatus Status
    {
        get => _project.Status;
        set => SetProperty(_project.Status, value, _project, (model, val) => model.Status = val);
    }

    public TimeLeaf.Models.Enums.ProjectHealth HealthStatus
    {
        get => _project.HealthStatus;
        set => SetProperty(_project.HealthStatus, value, _project, (model, val) => model.HealthStatus = val);
    }

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト。
    /// このリストの変更は、合計工数プロパティの変更を通知する。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> Tasks { get; } = new();

    /// <summary>
    /// プロジェクト全体の合計見積工数。
    /// </summary>
    public double TotalEstimatedCost => _project.TotalEstimatedCost;

    /// <summary>
    /// プロジェクト全体の合計実績工数。
    /// </summary>
    public double TotalActualCost => _project.TotalActualCost;

    /// <summary>
    /// UI表示用の合計見積工数文字列。
    /// </summary>
    public string DisplayTotalEstimatedCost => $"合計見積: {TotalEstimatedCost}";

    /// <summary>
    /// UI表示用の合計実績工数文字列。
    /// </summary>
    public string DisplayTotalActualCost => $"合計実績: {TotalActualCost}";


    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="project">ラップするProjectエンティティ。</param>
    public ProjectViewModel(Project project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));

        // ProjectTaskをProjectTaskViewModelでラップしてTasksコレクションに追加
        foreach (var task in _project.Tasks)
        {
            Tasks.Add(new ProjectTaskViewModel(task));
        }

        // ProjectエンティティのTasksコレクションの変更を購読し、UIのTasksコレクションを同期する
        _project.Tasks.CollectionChanged += OnProjectTasksCollectionChanged;

        // Tasksコレクションの変更を購読し、合計工数の変更を通知する
        Tasks.CollectionChanged += OnTasksCollectionChanged;
    }

    private void OnProjectTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            // Resetアクション（Clear()など）が発生した場合、VM側のコレクションもクリアする
            foreach (var ptvm in Tasks)
            {
                ptvm.PropertyChanged -= OnProjectTaskViewModelPropertyChanged;
            }
            Tasks.Clear();
            return;
        }

        if (e.NewItems != null)
        {
            foreach (ProjectTask newItem in e.NewItems)
            {
                var newPtvm = new ProjectTaskViewModel(newItem);
                Tasks.Add(newPtvm);
                newPtvm.PropertyChanged += OnProjectTaskViewModelPropertyChanged; // プロパティ変更を購読
            }
        }
        if (e.OldItems != null)
        {
            foreach (ProjectTask oldItem in e.OldItems)
            {
                var existing = Tasks.FirstOrDefault(ptvm => ptvm.Id == oldItem.Id);
                if (existing != null)
                {
                    existing.PropertyChanged -= OnProjectTaskViewModelPropertyChanged; // 購読解除
                    Tasks.Remove(existing);
                }
            }
        }
    }

    private void OnProjectTaskViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 個々のタスクのコストプロパティが変更された場合に、合計工数の変更を通知する
        if (e.PropertyName == nameof(ProjectTaskViewModel.EstimatedCost) ||
            e.PropertyName == nameof(ProjectTaskViewModel.ActualCost))
        {
            OnPropertyChanged(nameof(TotalEstimatedCost));
            OnPropertyChanged(nameof(TotalActualCost));
            OnPropertyChanged(nameof(DisplayTotalEstimatedCost)); // 追加
            OnPropertyChanged(nameof(DisplayTotalActualCost));   // 追加
        }
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // タスクの追加・削除があった場合、合計工数プロパティの変更をUIに通知する
        OnPropertyChanged(nameof(TotalEstimatedCost));
        OnPropertyChanged(nameof(TotalActualCost));
        OnPropertyChanged(nameof(DisplayTotalEstimatedCost)); // 追加
        OnPropertyChanged(nameof(DisplayTotalActualCost));   // 追加
    }

    // TODO: Tasks内の個々のタスクプロパティ変更（例: EstimatedCostの変更）も購読し、
    //       合計工数プロパティの変更を通知する必要があるが、これは次フェーズで実装する。
    //       現在のところ、タスクの追加・削除のみで合計を再計算・通知する。
}
