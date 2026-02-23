using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LoadProjectsUseCase _loadUseCase;
    private readonly SaveProjectUseCase _saveSingleUseCase;
    private readonly IProjectRepository _repository;
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool _isSyncing = false;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    /// <summary>
    /// 全プロジェクトのリスト（メモリ内保持）。
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="loadUseCase">プロジェクト読み込みユースケース。</param>
    /// <param name="saveSingleUseCase">単一プロジェクト保存ユースケース。</param>
    /// <param name="repository">イベント購読用リポジトリ（DIより注入）。</param>
    /// <param name="addProjectUseCase">プロジェクト追加ユースケース。</param>
    /// <param name="logger">ロガー。</param>
    /// <param name="serviceProvider">サービスプロバイダー。</param>
    public MainViewModel(LoadProjectsUseCase loadUseCase, SaveProjectUseCase saveSingleUseCase, IProjectRepository repository, IAddProjectUseCase addProjectUseCase, ILogger<MainViewModel> logger, IServiceProvider serviceProvider)
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _repository = repository;
        _addProjectUseCase = addProjectUseCase;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _currentViewModel = new OverviewViewModel(Projects, _addProjectUseCase, _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());

        _logger.LogInformation("MainViewModel Initializing");

        // 外部変更（同期）の監視
        _repository.ProjectChanged += OnProjectChanged;

        // 内部変更の監視と自動保存
        Projects.CollectionChanged += (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is ProjectViewModel itemViewModel)
                    {
                        WireProjectViewModelEvents(itemViewModel);
                    }
                }
            }
        };

        _ = InitializeAsync();

        _logger.LogInformation("MainViewModel Initializing Complete");
    }

    /// <summary>
    /// ProjectViewModel の変更（プロパティ、タスクリスト、タスクのプロパティ）を監視し、
    /// 自動保存を実行するようにイベントを購読します。
    /// </summary>
    private void WireProjectViewModelEvents(ProjectViewModel projectViewModel)
    {
        _logger.LogDebug("Wiring events for project {ProjectId}", projectViewModel.Id);

        // 保存をスキップする計算済みプロパティのリスト
        var ignoredProperties = new HashSet<string>
        {
            nameof(ProjectViewModel.TotalEstimatedCost),
            nameof(ProjectViewModel.TotalActualCost),
            nameof(ProjectViewModel.DisplayTotalEstimatedCost),
            nameof(ProjectViewModel.DisplayTotalActualCost)
        };

        // プロジェクト自体のプロパティ変更
        projectViewModel.PropertyChanged += async (s, e) =>
        {
            if (_isSyncing || e.PropertyName == null) return;
            if (ignoredProperties.Contains(e.PropertyName))
            {
                _logger.LogTrace("Skipping save for calculated project property: {PropertyName}", e.PropertyName);
                return;
            }

            _logger.LogTrace("Project property changed: {PropertyName}. Triggering save.", e.PropertyName);
            await AutoSaveProjectAsync(projectViewModel);
        };

        // タスクリストの変更
        projectViewModel.Tasks.CollectionChanged += async (s, e) =>
        {
            if (_isSyncing) return;
            _logger.LogTrace("Tasks collection changed. Triggering save.");

            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    if (newItem is ProjectTaskViewModel taskViewModel)
                    {
                        WireProjectTaskViewModelEvents(projectViewModel, taskViewModel);
                    }
                }
            }
            // 削除されたアイテムのイベント購読解除は、ViewModelが破棄されるか、
            // より厳密な管理が必要な場合に検討する。現状はLWWに基づき保存を優先。

            await AutoSaveProjectAsync(projectViewModel);
        };

        // マイルストーンリストの変更
        projectViewModel.Milestones.CollectionChanged += async (s, e) =>
        {
            if (_isSyncing) return;
            _logger.LogTrace("Milestones collection changed. Triggering save.");
            await AutoSaveProjectAsync(projectViewModel);
        };

        // 初期タスクのイベント購読
        foreach (var taskViewModel in projectViewModel.Tasks)
        {
            WireProjectTaskViewModelEvents(projectViewModel, taskViewModel);
        }
    }

    private void WireProjectTaskViewModelEvents(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel)
    {
        taskViewModel.PropertyChanged += async (s, e) =>
        {
            if (_isSyncing) return;
            _logger.LogTrace("Task property changed: {PropertyName}. Triggering save.", e.PropertyName);
            await AutoSaveProjectAsync(projectViewModel);
        };
    }

    private async System.Threading.Tasks.Task AutoSaveProjectAsync(ProjectViewModel projectViewModel)
    {
        try
        {
            await _saveSingleUseCase.ExecuteAsync(projectViewModel.Model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save project {ProjectId}", projectViewModel.Id);
        }
    }

    private void OnProjectChanged(Guid projectId)
    {
        _logger.LogInformation("Project changed event received for {ProjectId}", projectId);

        async System.Threading.Tasks.Task UpdateAction()
        {
            _isSyncing = true;
            try
            {
                var updatedProjectEntity = await _repository.LoadAsync(projectId);
                if (updatedProjectEntity == null) return;

                var existingViewModel = Projects.FirstOrDefault(pvm => pvm.Id == projectId); // ViewModelを検索
                if (existingViewModel != null)
                {
                    _logger.LogDebug("Updating existing project {ProjectId} ViewModel.", projectId);
                    existingViewModel.Name = updatedProjectEntity.Name; // ViewModel経由で更新
                    existingViewModel.Description = updatedProjectEntity.Description;
                    existingViewModel.Status = updatedProjectEntity.Status;
                    existingViewModel.HealthStatus = updatedProjectEntity.HealthStatus;

                    // Tasksコレクションの同期
                    // Model.Tasks の Clear() により VM.Tasks も同期してクリアされるため、
                    // VM.Tasks.Clear() の直接呼び出しは不要。
                    existingViewModel.Model.Tasks.Clear();
                    foreach (var t in updatedProjectEntity.Tasks)
                    {
                        existingViewModel.Model.Tasks.Add(t);
                    }
                }
                else
                {
                    _logger.LogDebug("Adding new project {ProjectId} ViewModel from sync.", projectId);
                    var newProjectViewModel = new ProjectViewModel(updatedProjectEntity);
                    Projects.Add(newProjectViewModel); // Projects.CollectionChanged によって WireProjectViewModelEvents が呼ばれる
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project {ProjectId} on change event.", projectId);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // Dispatcher を介して実行（UIスレッドを担保）
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.InvokeAsync(UpdateAction);
        }
        else
        {
            _ = UpdateAction();
        }
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        _logger.LogInformation("Loading initial projects");
        _isSyncing = true;
        try
        {
            var projectEntities = await _loadUseCase.ExecuteAsync();
            foreach (var projectEntity in projectEntities)
            {
                var projectViewModel = new ProjectViewModel(projectEntity);
                Projects.Add(projectViewModel); // Projects.CollectionChanged によって WireProjectViewModelEvents が呼ばれる
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize projects");
        }
        finally
        {
            _isSyncing = false;
        }
        _logger.LogInformation("Loading initial projects Complete");
    }

    [RelayCommand]
    private void NavigateToProject(ProjectViewModel projectViewModel) // 引数の型を ViewModel に変更
    {
        if (projectViewModel == null) return;
        _logger.LogInformation("Navigating to project {ProjectId}", projectViewModel.Id);
        CurrentViewModel = new ProjectWorkspaceViewModel(projectViewModel, _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>()); // ViewModel を渡す
    }

    [RelayCommand]
    private void NavigateBack()
    {
        _logger.LogInformation("Navigating back to Overview");
        CurrentViewModel = new OverviewViewModel(Projects, _addProjectUseCase, _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());
    }
}
