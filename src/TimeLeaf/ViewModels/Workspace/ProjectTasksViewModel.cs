using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスク移動コマンドの引数。
/// </summary>
/// <param name="Task">移動対象のタスク。</param>
/// <param name="NewParentId">移動先のコンテナID（nullの場合は未分類）。</param>
public record MoveTaskArgs(ProjectTaskViewModel Task, Guid? NewParentId);

/// <summary>
/// プロジェクトのタスク一覧と操作を担当するViewModel。
/// </summary>
public partial class ProjectTasksViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IAddTaskUseCase _addTaskUseCase;
    private readonly IAddContainerUseCase _addContainerUseCase;
    private readonly IMoveTaskUseCase _moveTaskUseCase;
    private readonly IDeleteTaskUseCase _deleteTaskUseCase;
    private readonly IDeleteContainerUseCase _deleteContainerUseCase;
    private readonly IGetProjectMembersUseCase _getProjectMembersUseCase;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly LeafKit.UI.Services.IDialogService _dialogService;
    private readonly ILogger<ProjectTasksViewModel> _logger;
    private readonly ILogger<TaskDetailViewModel> _detailLogger;
    private readonly DetectProjectRisksUseCase _detectRisksUseCase;
    private readonly CalculateFlowLayoutUseCase _layoutUseCase;

    /// <summary>
    /// タスク詳細の表示がリクエストされたときに発生するイベント。
    /// </summary>
    public event EventHandler<ProjectTaskViewModel>? TaskDetailRequested;

    public ObservableCollection<ProjectTaskViewModel> Tasks => _projectViewModel.Tasks;

    [ObservableProperty]
    private ProjectTaskViewModel? _selectedTask;

    [ObservableProperty]
    private bool _isDetailVisible;

    [ObservableProperty]
    private ObservableCollection<ProjectRisk> _risks = new();

    [ObservableProperty]
    private string _weatherIcon = "☀️";

    [ObservableProperty]
    private string _weatherMessage = "順調です";

    [ObservableProperty]
    private ObservableCollection<TaskEdgeViewModel> _edges = new();

    /// <summary>
    /// 移動先候補となるコンテナのリスト。
    /// </summary>
    public ObservableCollection<ProjectContainer> AvailableContainers => new(_projectViewModel.Model.Containers);

    public ObservableCollection<TaskContainerViewModel> TaskContainers { get; } = new();

    public ProjectTasksViewModel(
        ProjectViewModel projectViewModel,
        IAddTaskUseCase addTaskUseCase,
        IAddContainerUseCase addContainerUseCase,
        IMoveTaskUseCase moveTaskUseCase,
        IDeleteTaskUseCase deleteTaskUseCase,
        IDeleteContainerUseCase deleteContainerUseCase,
        IGetProjectMembersUseCase getProjectMembersUseCase,
        IViewModelFactory viewModelFactory,
        LeafKit.UI.Services.IDialogService dialogService,
        DetectProjectRisksUseCase detectRisksUseCase,
        CalculateFlowLayoutUseCase layoutUseCase,
        ILogger<ProjectTasksViewModel> logger,
        ILogger<TaskDetailViewModel> detailLogger
    )
    {
        _projectViewModel = projectViewModel;
        _addTaskUseCase = addTaskUseCase;
        _addContainerUseCase = addContainerUseCase;
        _moveTaskUseCase = moveTaskUseCase;
        _deleteTaskUseCase = deleteTaskUseCase;
        _deleteContainerUseCase = deleteContainerUseCase;
        _getProjectMembersUseCase = getProjectMembersUseCase;
        _viewModelFactory = viewModelFactory;
        _dialogService = dialogService;
        _detectRisksUseCase = detectRisksUseCase;
        _layoutUseCase = layoutUseCase;
        _logger = logger;
        _detailLogger = detailLogger;

        InitializeNodePositions();
        SyncEdges();
        RebuildContainers();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task MoveTask(MoveTaskArgs args)
    {
        if (args == null || args.Task == null)
            return;

        try
        {
            _logger.LogInformation("Moving task {TaskId} to parent {ParentId}", args.Task.Id, args.NewParentId);

            await _moveTaskUseCase.ExecuteAsync(_projectViewModel.Model, args.Task.Id, args.NewParentId);

            // UIの状態を同期
            _projectViewModel.SyncFromModel();
            RebuildContainers();

            // リスクのスキャンも再実行（階層変更による制約への影響を考慮）
            await ScanRisksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move task.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteTask(ProjectTaskViewModel? task)
    {
        if (task == null)
            return;

        try
        {
            // TODO: 確認ダイアログの表示を検討（現在は即時削除）
            _logger.LogInformation("Deleting task {TaskId}: {TaskName}", task.Id, task.Name);

            await _deleteTaskUseCase.ExecuteAsync(_projectViewModel.Model, task.Id);

            // UIの状態を同期
            _projectViewModel.SyncFromModel();
            RebuildContainers();

            // リスクのスキャンも再実行
            await ScanRisksAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteContainer(TaskContainerViewModel? container)
    {
        if (container == null || container.IsUnclassified)
            return;

        try
        {
            var message =
                $"コンテナ '{container.DisplayName}' を削除しますか？\nコンテナ内のすべてのタスクも削除されます。";
            var result = _dialogService.ShowConfirmationDialog(message, "コンテナの削除");

            if (result)
            {
                _logger.LogInformation(
                    "Deleting container {ContainerId}: {ContainerName}",
                    container.Id,
                    container.DisplayName
                );

                await _deleteContainerUseCase.ExecuteAsync(_projectViewModel.Model, container.Id);

                // UIの状態を同期
                _projectViewModel.SyncFromModel();
                RebuildContainers();

                // リスクのスキャンも再実行
                await ScanRisksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete container.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ReorderItems()
    {
        try
        {
            // 並べ替え対象のアイテムを収集（コンテナと、未分類タスクのリスト）
            var allItems = new List<ProjectWorkItem>();
            foreach (var cvm in TaskContainers)
            {
                // TaskContainerViewModel がラップしている Entity を抽出する必要があるが、
                // 現時点では ProjectContainer? _container が private なので、
                // 適切にアクセスするためのプロパティが必要になるかもしれない。
                // いったん、現在のドメインから直接取得して並べ替える。
                // (本来は ViewModel 側で完結すべきだが、今回はシンプルにドメインから構築)
            }

            // シンプルなアプローチ: 全アイテムを取得して並べ替える
            var targetItems = _projectViewModel
                .Model.Containers.Cast<ProjectWorkItem>()
                .Concat(_projectViewModel.Model.Tasks.Where(t => t.ParentId == null).Cast<ProjectWorkItem>())
                .ToList();

            var reorderVm = _viewModelFactory.CreateReorderWorkItemsViewModel(_projectViewModel.Model, targetItems);
            var result = await _dialogService.ShowDialogAsync(reorderVm);

            if (result)
            {
                _projectViewModel.SyncFromModel();
                RebuildContainers();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder items.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddContainer(TaskContainerViewModel? container = null)
    {
        try
        {
            var addContainerVm = _viewModelFactory.CreateAddContainerViewModel();
            var result = await _dialogService.ShowDialogAsync(addContainerVm);

            if (result)
            {
                var parentId = container?.Id != Guid.Empty ? container?.Id : null;

                await _addContainerUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addContainerVm.Name,
                    addContainerVm.Description,
                    parentId
                );

                _projectViewModel.SyncFromModel();
                RebuildContainers();
                await ScanRisksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add container.");
        }
    }

    private void RebuildContainers()
    {
        TaskContainers.Clear();
        var allTasks = Tasks.ToList();

        // 1. プロジェクトが保持するコンテナエンティティを基点に表示リストを作成
        foreach (var container in _projectViewModel.Model.Containers)
        {
            // このコンテナに属するタスクを抽出
            var children = allTasks.Where(t => t.ParentId == container.Id).ToList();

            TaskContainers.Add(
                new TaskContainerViewModel(
                    container,
                    new ObservableCollection<ProjectTaskViewModel>(children),
                    AddTaskToContainerCommand,
                    DeleteContainerCommand
                )
            );
        }

        // 2. どのコンテナにも属さない「未分類タスク」を最後に追加
        var containerIds = _projectViewModel.Model.Containers.Select(c => c.Id).ToHashSet();
        var unclassified = allTasks
            .Where(t => !t.ParentId.HasValue || !containerIds.Contains(t.ParentId.Value))
            .ToList();

        if (unclassified.Any())
        {
            TaskContainers.Add(
                new TaskContainerViewModel(
                    null,
                    new ObservableCollection<ProjectTaskViewModel>(unclassified),
                    AddTaskToContainerCommand,
                    DeleteContainerCommand
                )
            );
        }
    }

    private void InitializeNodePositions()
    {
        var layout = _layoutUseCase.Execute(_projectViewModel.Model);

        foreach (var t in Tasks)
        {
            if (layout.TryGetValue(t.Id, out var pos))
            {
                t.X = pos.X;
                t.Y = pos.Y;
            }
        }
    }

    private void SyncEdges()
    {
        Edges.Clear();
        var vmMap = Tasks.ToDictionary(t => t.Id);

        foreach (var task in Tasks)
        {
            foreach (var constraint in task.Constraints)
            {
                if (vmMap.TryGetValue(constraint.PredecessorId, out var predecessor))
                {
                    Edges.Add(new TaskEdgeViewModel(predecessor, task));
                }
            }
        }
    }

    /// <summary>
    /// プロジェクトのリスクをスキャンして UI を更新します。
    /// </summary>
    public async System.Threading.Tasks.Task ScanRisksAsync()
    {
        try
        {
            var foundRisks = await _detectRisksUseCase.ExecuteAsync(_projectViewModel.Model);

            // UIスレッドでの更新（ObservableCollectionのため）
            Risks.Clear();
            foreach (var risk in foundRisks)
            {
                Risks.Add(risk);
            }

            // 各タスクViewModelのリスクフラグを更新
            var riskTaskIds = new HashSet<Guid>(Risks.Where(r => r.TaskId.HasValue).Select(r => r.TaskId!.Value));
            foreach (var task in Tasks)
            {
                task.HasRisk = riskTaskIds.Contains(task.Id);
            }

            // 天気アイコンの更新
            UpdateWeather();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan risks.");
        }
    }

    private void UpdateWeather()
    {
        if (Risks.Any(r => r.IsError))
        {
            WeatherIcon = "⚡";
            WeatherMessage = "深刻な問題が検出されました";
        }
        else if (Risks.Any())
        {
            WeatherIcon = "☁️";
            WeatherMessage = "リスクが予報されています";
        }
        else
        {
            WeatherIcon = "☀️";
            WeatherMessage = "順調に成長しています";
        }
    }

    partial void OnSelectedTaskChanged(ProjectTaskViewModel? value)
    {
        foreach (var task in Tasks)
        {
            task.IsSelected = (task == value);
        }

        if (value != null)
        {
            IsDetailVisible = true;
        }
        else
        {
            IsDetailVisible = false;
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddTaskToContainer(TaskContainerViewModel? container)
    {
        try
        {
            var teammates = await _getProjectMembersUseCase.ExecuteAsync(_projectViewModel.Model);
            var addTaskVm = _viewModelFactory.CreateAddTaskViewModel(teammates);
            var result = await _dialogService.ShowDialogAsync(addTaskVm);

            if (result)
            {
                var assigneeId = addTaskVm.Assignee?.Id;
                var parentId = container?.Id != Guid.Empty ? container?.Id : null;

                await _addTaskUseCase.ExecuteAsync(
                    _projectViewModel.Model,
                    addTaskVm.Name,
                    addTaskVm.Description,
                    addTaskVm.Status,
                    addTaskVm.Priority,
                    parentId,
                    null, // scheduledStartDate
                    addTaskVm.DueDate,
                    null, // actualStartDate
                    null, // actualEndDate
                    addTaskVm.EstimatedWorkHours ?? 0,
                    0, // actualCost
                    assigneeId
                );

                _projectViewModel.SyncFromModel();
                RebuildContainers();
                await ScanRisksAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add task to container.");
        }
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task AddTask() => await AddTaskToContainer(null);

    [RelayCommand]
    private void OpenTaskDetailWindow(ProjectTaskViewModel task)
    {
        TriggerTaskDetailRequested(task);
    }

    public void TriggerTaskDetailRequested(ProjectTaskViewModel task)
    {
        _logger.LogInformation("Requesting task detail for: {TaskName}", task.Name);
        TaskDetailRequested?.Invoke(this, task);
    }
}
