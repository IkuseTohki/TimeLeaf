using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly ILoadProjectsUseCase _loadUseCase;
    private readonly ISaveProjectUseCase _saveSingleUseCase;
    private readonly IFindProjectUseCase _findProjectUseCase;
    private readonly IProjectSyncService _syncService;
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<MainViewModel> _logger;

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
    public MainViewModel(
        ILoadProjectsUseCase loadUseCase,
        ISaveProjectUseCase saveSingleUseCase,
        IFindProjectUseCase findProjectUseCase,
        IProjectSyncService syncService,
        IAddProjectUseCase addProjectUseCase,
        IViewModelFactory viewModelFactory,
        ILogger<MainViewModel> logger)
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _findProjectUseCase = findProjectUseCase;
        _syncService = syncService;
        _addProjectUseCase = addProjectUseCase;
        _viewModelFactory = viewModelFactory;
        _logger = logger;

        _currentViewModel = _viewModelFactory.CreateOverviewViewModel(Projects);

        _logger.LogInformation("MainViewModel Initializing");

        // 外部変更（同期）の監視をサービス経由で行う
        _syncService.ProjectChanged += OnProjectChanged;

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
    /// ProjectViewModel の変更を監視し、ドメインルール（UpdatedAtの更新）に基づいて
    /// 自動保存を実行するようにイベントを購読します。
    /// </summary>
    private void WireProjectViewModelEvents(ProjectViewModel projectViewModel)
    {
        _logger.LogDebug("Wiring events for project {ProjectId}", projectViewModel.Id);

        projectViewModel.PropertyChanged += async (sender, e) =>
        {
            if (_isSyncing) return;

            // UpdatedAt が変更された = ドメイン層で何らかの重要な変更があったとみなす
            // ドメインエンティティのビジネスメソッドはすべてこれを更新するため、
            // これ一つを監視するだけで整合性を保った自動保存が可能。
            if (e.PropertyName == nameof(ProjectViewModel.UpdatedAt))
            {
                await AutoSaveProjectAsync(projectViewModel);
            }
        };
    }

    private async System.Threading.Tasks.Task AutoSaveProjectAsync(ProjectViewModel projectViewModel)
    {
        _logger.LogInformation("Auto-save triggered for project {ProjectId} ({ProjectName})", projectViewModel.Id, projectViewModel.Name);
        try
        {
            await _saveSingleUseCase.ExecuteAsync(projectViewModel.Model);
            _logger.LogInformation("Auto-save completed for project {ProjectId}", projectViewModel.Id);
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
                var updatedProjectEntity = await _findProjectUseCase.ExecuteAsync(projectId);
                if (updatedProjectEntity == null) return;

                var existingViewModel = Projects.FirstOrDefault(pvm => pvm.Id == projectId);
                if (existingViewModel != null)
                {
                    existingViewModel.IsSyncing = true;
                    try
                    {
                        _logger.LogDebug("Updating existing project {ProjectId} ViewModel.", projectId);
                        existingViewModel.Model.UpdateName(updatedProjectEntity.Name);
                        existingViewModel.Model.UpdateDescription(updatedProjectEntity.Description);
                        existingViewModel.Model.UpdateStatus(updatedProjectEntity.Status);
                        existingViewModel.Model.UpdateHealth(updatedProjectEntity.HealthStatus);
                        existingViewModel.Model.SetUpdatedAt(updatedProjectEntity.UpdatedAt);

                        // Tasksの同期
                        existingViewModel.Model.ClearTasks();
                        foreach (var t in updatedProjectEntity.Tasks)
                        {
                            existingViewModel.Model.AddTask(t);
                        }

                        existingViewModel.SyncFromModel();
                    }
                    finally
                    {
                        existingViewModel.IsSyncing = false;
                    }
                }
                else
                {
                    _logger.LogDebug("Adding new project {ProjectId} ViewModel from sync.", projectId);
                    var newProjectViewModel = new ProjectViewModel(updatedProjectEntity);
                    Projects.Add(newProjectViewModel);
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

        // Application.Current.Dispatcher が利用可能な場合はそれを使用し、
        // テスト環境などで利用不可な場合は直接実行する。
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.InvokeAsync(UpdateAction);
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
                projectViewModel.IsSyncing = true;
                try
                {
                    Projects.Add(projectViewModel); // Projects.CollectionChanged によって WireProjectViewModelEvents が呼ばれる
                }
                finally
                {
                    projectViewModel.IsSyncing = false;
                }
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
    private void NavigateToProject(ProjectViewModel projectViewModel)
    {
        if (projectViewModel == null) return;
        _logger.LogInformation("Navigating to project {ProjectId}", projectViewModel.Id);
        CurrentViewModel = _viewModelFactory.CreateProjectWorkspaceViewModel(projectViewModel);
    }

    [RelayCommand]
    private void NavigateBack()
    {
        _logger.LogInformation("Navigating back to Overview");
        CurrentViewModel = _viewModelFactory.CreateOverviewViewModel(Projects);
    }
}
