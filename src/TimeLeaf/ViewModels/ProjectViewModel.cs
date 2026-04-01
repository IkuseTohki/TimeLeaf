using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// Projectエンティティをラップし、UIバインディングのための通知機能を提供する ViewModel。
/// </summary>
public partial class ProjectViewModel : ObservableObject
{
    private Project _project;
    private readonly Guid _currentUserId;
    private readonly IJoinProjectUseCase _joinProjectUseCase;
    private readonly IViewModelFactory _viewModelFactory;

    /// <summary>
    /// 同期中（外部からの読み込み中）かどうかを示すフラグ。
    /// この間は UpdatedAt の自動更新を停止します。
    /// </summary>
    [ObservableProperty]
    private bool _isSyncing;

    /// <summary>
    /// 基になる Project エンティティ。
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

                // 全タスクのプロジェクト名を更新
                foreach (var task in Tasks)
                {
                    task.ProjectName = value;
                }

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
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 最終更新日時。
    /// </summary>
    public DateTime UpdatedAt
    {
        get => _project.UpdatedAt;
        set
        {
            if (_project.UpdatedAt != value)
            {
                _project.SetUpdatedAt(value.ToUniversalTime());
                OnPropertyChanged(nameof(UpdatedAt));
                OnPropertyChanged(nameof(DisplayLastUpdated));
            }
        }
    }

    /// <summary>
    /// UI 表示用の最終更新日時文字列。
    /// </summary>
    public string DisplayLastUpdated
    {
        get
        {
            var utcNow = DateTime.UtcNow;
            var diff = utcNow - UpdatedAt.ToUniversalTime();
            var localUpdatedAt = UpdatedAt.ToLocalTime();

            if (diff.TotalSeconds < 0) return localUpdatedAt.ToString("yyyy/MM/dd HH:mm");
            if (diff.TotalSeconds < 60) return "たった今";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}分前";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}時間前";
            return localUpdatedAt.ToString("yyyy/MM/dd HH:mm");
        }
    }

    /// <summary>
    /// プロジェクトのマイルストーン（UI 用）。
    /// </summary>
    public ObservableCollection<Milestone> Milestones { get; } = new();

    /// <summary>
    /// プロジェクトに紐づくタスクのリスト（UI 用）。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> Tasks { get; } = new();

    /// <summary>
    /// プロジェクト全体の合計見積工数。
    /// </summary>
    public double TotalEstimatedCost { get => _project.TotalEstimatedCost; set { } }

    /// <summary>
    /// プロジェクト全体の合計実績工数。
    /// </summary>
    public double TotalActualCost { get => _project.TotalActualCost; set { } }

    /// <summary>
    /// UI 表示用の合計見積工数文字列。
    /// </summary>
    public string DisplayTotalEstimatedCost { get => $"合計見積: {TotalEstimatedCost}"; set { } }

    /// <summary>
    /// UI 表示用の合計実績工数文字列。
    /// </summary>
    public string DisplayTotalActualCost { get => $"合計実績: {TotalActualCost}"; set { } }

    /// <summary>
    /// タスクの総数。
    /// </summary>
    public int TotalTaskCount { get => _project.Tasks.Count; set { } }

    /// <summary>
    /// 完了済みタスクの数。
    /// </summary>
    public int CompletedTaskCount { get => _project.Tasks.Count(t => t.Status == TimeLeaf.Models.Enums.TaskStatus.Completed); set { } }

    /// <summary>
    /// 全体の進捗率 (0-100)。
    /// </summary>
    public double CompletionPercentage { get => TotalTaskCount == 0 ? 0 : (double)CompletedTaskCount / TotalTaskCount * 100; set { } }

    /// <summary>
    /// 現在のユーザー（自分）がこのプロジェクトにアサインされているかどうか。
    /// </summary>
    public bool IsAssignedToMe => _project.AssignedUserIds.Contains(_currentUserId);

    /// <summary>
    /// プロジェクトに参加するコマンド。
    /// </summary>
    public IAsyncRelayCommand JoinProjectCommand { get; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="project">ラップする Project エンティティ。</param>
    /// <param name="currentUserId">現在のユーザー ID。</param>
    /// <param name="joinProjectUseCase">プロジェクトに参加するためのユースケース。</param>
    /// <param name="viewModelFactory">ViewModel を生成するためのファクトリ。</param>
    public ProjectViewModel(Project project, Guid currentUserId, IJoinProjectUseCase joinProjectUseCase, IViewModelFactory viewModelFactory)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
        _currentUserId = currentUserId;
        _joinProjectUseCase = joinProjectUseCase ?? throw new ArgumentNullException(nameof(joinProjectUseCase));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));

        JoinProjectCommand = new AsyncRelayCommand(ExecuteJoinProjectAsync, () => !IsAssignedToMe);

        // 初期データのロード
        SyncFromModel();
    }

    private async Task ExecuteJoinProjectAsync()
    {
        try
        {
            await _joinProjectUseCase.ExecuteAsync(_project);
            // モデルが更新されたので同期（IsAssignedToMe が更新される）
            SyncFromModel();
            JoinProjectCommand.NotifyCanExecuteChanged();
        }
        catch (Exception)
        {
            // TODO: エラー通知
        }
    }

    /// <summary>
    /// モデルの状態を最新のエンティティで更新し、UI に同期します。
    /// </summary>
    /// <param name="newModel">最新の状態を持つエンティティ。</param>
    public void UpdateFromModel(Project newModel)
    {
        if (newModel == null) throw new ArgumentNullException(nameof(newModel));
        if (newModel.Id != _project.Id) throw new ArgumentException("Cannot update ViewModel with a different Project ID.");

        _project = newModel;
        SyncFromModel();
    }

    /// <summary>
    /// モデルの状態を UI コレクションに同期します。
    /// </summary>
    public void SyncFromModel()
    {
        IsSyncing = true;
        try
        {
            // 1. タスクの差分同期
            var modelTaskIds = _project.Tasks.Select(t => t.Id).ToHashSet();

            // 削除されたタスクの除去
            var tasksToRemove = Tasks.Where(vm => !modelTaskIds.Contains(vm.Id)).ToList();
            foreach (var vm in tasksToRemove)
            {
                vm.PropertyChanged -= OnProjectTaskViewModelPropertyChanged;
                Tasks.Remove(vm);
            }

            // 追加または更新（順序を維持）
            for (int i = 0; i < _project.Tasks.Count; i++)
            {
                var taskModel = _project.Tasks[i];
                var existingVm = Tasks.FirstOrDefault(vm => vm.Id == taskModel.Id);

                if (existingVm != null)
                {
                    // 既存: 中身を更新
                    existingVm.UpdateFromModel(taskModel);
                    existingVm.ProjectName = Name;

                    // 並び順が違う場合は移動
                    var currentIndex = Tasks.IndexOf(existingVm);
                    if (currentIndex != i)
                    {
                        Tasks.Move(currentIndex, i);
                    }
                }
                else
                {
                    // 新規: インスタンス作成
                    var newTaskVm = _viewModelFactory.CreateProjectTaskViewModel(taskModel);
                    newTaskVm.ProjectName = Name;
                    newTaskVm.PropertyChanged += OnProjectTaskViewModelPropertyChanged;
                    Tasks.Insert(i, newTaskVm);
                }
            }

            // 2. マイルストーンの差分同期 (Milestone は Record なので単純な入れ替えを避ける)
            if (!Milestones.SequenceEqual(_project.Milestones))
            {
                Milestones.Clear();
                foreach (var m in _project.Milestones)
                {
                    Milestones.Add(m);
                }
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
        OnPropertyChanged(nameof(IsAssignedToMe));
    }

    private void OnProjectTaskViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsSyncing) return;

        // 個々のタスクのプロパティ（コスト、担当者、ステータス等）が変更された場合
        // 親である ProjectViewModel の Tasks プロパティが変更されたとみなして通知する。
        // これにより MainViewModel の自動保存がトリガーされる。
        OnPropertyChanged(nameof(Tasks));

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
        OnPropertyChanged(nameof(IsAssignedToMe));
    }
}
