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
    /// 同期中（外部からの読み込み中）かどうかを示すフラグ。
    /// この間は UpdatedAt の自動更新を停止します。
    /// </summary>
    [ObservableProperty]
    private bool _isSyncing;

    /// <summary>
    /// 基になるProjectエンティティ。
    /// </summary>
    public Project Model => _project;

    public Guid Id => _project.Id;

    public string Name
    {
        get => _project.Name;
        set
        {
            if (SetProperty(_project.Name, value, _project, (model, val) => model.Name = val))
            {
                RefreshUpdatedAt();
            }
        }
    }

    public string Description
    {
        get => _project.Description;
        set
        {
            if (SetProperty(_project.Description, value, _project, (model, val) => model.Description = val))
            {
                RefreshUpdatedAt();
            }
        }
    }

    public TimeLeaf.Models.Enums.ProjectStatus Status
    {
        get => _project.Status;
        set
        {
            if (SetProperty(_project.Status, value, _project, (model, val) => model.Status = val))
            {
                RefreshUpdatedAt();
            }
        }
    }

    public TimeLeaf.Models.Enums.ProjectHealth HealthStatus
    {
        get => _project.HealthStatus;
        set
        {
            if (SetProperty(_project.HealthStatus, value, _project, (model, val) => model.HealthStatus = val))
            {
                RefreshUpdatedAt();
            }
        }
    }

    private void RefreshUpdatedAt()
    {
        if (IsSyncing) return;
        UpdatedAt = DateTime.Now;
    }

    /// <summary>
    /// 最終更新日時。
    /// </summary>
    public DateTime UpdatedAt
    {
        get => _project.UpdatedAt;
        set
        {
            if (SetProperty(_project.UpdatedAt, value, _project, (model, val) => model.UpdatedAt = val))
            {
                OnPropertyChanged(nameof(DisplayLastUpdated));
            }
        }
    }

    /// <summary>
    /// UI表示用の最終更新日時文字列。
    /// </summary>
    public string DisplayLastUpdated
    {
        get
        {
            var diff = DateTime.Now - UpdatedAt;
            if (diff.TotalSeconds < 60) return "たった今";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}分前";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}時間前";
            return UpdatedAt.ToString("yyyy/MM/dd HH:mm");
        }
    }

    /// <summary>
    /// プロジェクトのマイルストーン。
    /// </summary>
    public ObservableCollection<Milestone> Milestones => _project.Milestones;

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
            var taskVm = new ProjectTaskViewModel(task);
            Tasks.Add(taskVm);
            taskVm.PropertyChanged += OnProjectTaskViewModelPropertyChanged; // イベント購読を追加
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
            foreach (var item in e.NewItems)
            {
                if (item is ProjectTask newItem)
                {
                    var newPtvm = new ProjectTaskViewModel(newItem);
                    Tasks.Add(newPtvm);
                    newPtvm.PropertyChanged += OnProjectTaskViewModelPropertyChanged; // プロパティ変更を購読
                }
            }
        }
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is ProjectTask oldItem)
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
            RefreshUpdatedAt();
        }
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // タスクの追加・削除があった場合、合計工数プロパティの変更をUIに通知する
        OnPropertyChanged(nameof(TotalEstimatedCost));
        OnPropertyChanged(nameof(TotalActualCost));
        OnPropertyChanged(nameof(DisplayTotalEstimatedCost)); // 追加
        OnPropertyChanged(nameof(DisplayTotalActualCost));   // 追加
        RefreshUpdatedAt();
    }

    // TODO: Tasks内の個々のタスクプロパティ変更（例: EstimatedCostの変更）も購読し、
    //       合計工数プロパティの変更を通知する必要があるが、これは次フェーズで実装する。
    //       現在のところ、タスクの追加・削除のみで合計を再計算・通知する。
}
