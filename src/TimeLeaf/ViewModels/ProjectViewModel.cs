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
            if (_project.Name != value)
            {
                _project.UpdateName(value);
                OnPropertyChanged(nameof(Name));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(DisplayLastUpdated));
            }
        }
    }

    public string Description
    {
        get => _project.Description;
        set
        {
            if (_project.Description != value)
            {
                _project.UpdateDescription(value);
                OnPropertyChanged(nameof(Description));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(DisplayLastUpdated));
            }
        }
    }

    public TimeLeaf.Models.Enums.ProjectStatus Status
    {
        get => _project.Status;
        set
        {
            if (_project.Status != value)
            {
                _project.UpdateStatus(value);
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(DisplayLastUpdated));
            }
        }
    }

    public TimeLeaf.Models.Enums.ProjectHealth HealthStatus
    {
        get => _project.HealthStatus;
        set
        {
            if (_project.HealthStatus != value)
            {
                _project.UpdateHealth(value);
                OnPropertyChanged(nameof(HealthStatus));
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(DisplayLastUpdated));
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
    /// プロジェクトのマイルストーン（UI用）。
    /// TODO: マイルストーンの追加・削除もドメインメソッド経由に変更し、このコレクションを同期させる。
    /// </summary>
    public ObservableCollection<Milestone> Milestones { get; } = new();

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト（UI用）。
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

        // 初期データのロード
        SyncFromModel();
    }

    /// <summary>
    /// モデルの状態をUIコレクションに同期します。
    /// </summary>
    public void SyncFromModel()
    {
        IsSyncing = true;
        try
        {
            // タスクの同期
            foreach (var t in Tasks) t.PropertyChanged -= OnProjectTaskViewModelPropertyChanged;
            Tasks.Clear();
            foreach (var task in _project.Tasks)
            {
                var taskVm = new ProjectTaskViewModel(task);
                Tasks.Add(taskVm);
                taskVm.PropertyChanged += OnProjectTaskViewModelPropertyChanged;
            }

            // マイルストーンの同期
            Milestones.Clear();
            foreach (var m in _project.Milestones)
            {
                Milestones.Add(m);
            }

            NotifyAllProperties();
        }
        finally
        {
            IsSyncing = false;
        }
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Description));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(HealthStatus));
        OnPropertyChanged(nameof(UpdatedAt));
        OnPropertyChanged(nameof(DisplayLastUpdated));
        OnPropertyChanged(nameof(TotalEstimatedCost));
        OnPropertyChanged(nameof(TotalActualCost));
        OnPropertyChanged(nameof(DisplayTotalEstimatedCost));
        OnPropertyChanged(nameof(DisplayTotalActualCost));
    }

    private void OnProjectTaskViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 個々のタスクのプロパティ（コスト、担当者、ステータス等）が変更された場合に、
        // プロジェクト全体の最終更新日時を更新し、通知を行う。
        // これにより MainViewModel の自動保存がトリガーされる。
        _project.RefreshUpdatedAt();
        OnPropertyChanged(nameof(UpdatedAt));
        OnPropertyChanged(nameof(DisplayLastUpdated));

        if (e.PropertyName == nameof(ProjectTaskViewModel.EstimatedCost) ||
            e.PropertyName == nameof(ProjectTaskViewModel.ActualCost))
        {
            NotifyTotalCosts();
        }
    }

    private void NotifyTotalCosts()
    {
        OnPropertyChanged(nameof(TotalEstimatedCost));
        OnPropertyChanged(nameof(TotalActualCost));
        OnPropertyChanged(nameof(DisplayTotalEstimatedCost));
        OnPropertyChanged(nameof(DisplayTotalActualCost));
        OnPropertyChanged(nameof(UpdatedAt));
        OnPropertyChanged(nameof(DisplayLastUpdated));
    }
}
