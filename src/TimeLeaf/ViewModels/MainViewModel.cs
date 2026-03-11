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
using LeafKit.UI.Services;

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
    private readonly IProjectSaveCoordinator _saveCoordinator;
    private readonly IDispatcherService _dispatcherService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly INotificationService _notificationService;
    private readonly ISnackbarService _snackbarService;
    private readonly IOSNotificationService _osNotificationService;
    private readonly ICheckTaskDeadlinesUseCase _checkDeadlinesUseCase;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    [ObservableProperty]
    private int _unreadNotificationCount;

    /// <summary>
    /// メインウィンドウが表示されているかどうか。
    /// </summary>
    [ObservableProperty]
    private bool _isWindowVisible = true;

    /// <summary>
    /// アプリケーションを完全に終了してもよいかどうか。
    /// false の場合はウィンドウを閉じる代わりに隠す挙動になります。
    /// </summary>
    public bool CanExit { get; private set; } = false;

    /// <summary>
    /// アプリケーションの終了が要求されたときに発生します。
    /// </summary>
    public event EventHandler? ExitRequested;

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
        IProjectSaveCoordinator saveCoordinator,
        IDispatcherService dispatcherService,
        IViewModelFactory viewModelFactory,
        INotificationService notificationService,
        ISnackbarService snackbarService,
        IOSNotificationService osNotificationService,
        ICheckTaskDeadlinesUseCase checkDeadlinesUseCase,
        ILogger<MainViewModel> logger)
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _findProjectUseCase = findProjectUseCase;
        _syncService = syncService;
        _addProjectUseCase = addProjectUseCase;
        _saveCoordinator = saveCoordinator;
        _dispatcherService = dispatcherService;
        _viewModelFactory = viewModelFactory;
        _notificationService = notificationService;
        _snackbarService = snackbarService;
        _osNotificationService = osNotificationService;
        _checkDeadlinesUseCase = checkDeadlinesUseCase;
        _logger = logger;

        _currentViewModel = _viewModelFactory.CreateOverviewViewModel(Projects);

        _logger.LogInformation("MainViewModel Initializing");

        // 外部変更（同期）の監視をサービス経由で行う
        _syncService.ProjectChanged += OnProjectChanged;

        // 通知センターとの同期
        _notificationService.UnreadCountChanged += (s, e) => UpdateUnreadCount();
        _notificationService.NotificationAdded += (s, n) =>
        {
            _snackbarService.Show($"{n.Title}: {n.Message}");
            _osNotificationService.Show(n.Title, n.Message);
        };
        UpdateUnreadCount();

        // トレイイベントの購読
        _osNotificationService.RequestOpen += (s, e) => IsWindowVisible = true;
        _osNotificationService.RequestExit += (s, e) =>
        {
            CanExit = true;
            ExitRequested?.Invoke(this, EventArgs.Empty);
        };

        // 自動保存の開始
        _saveCoordinator.StartMonitoring(Projects);

        _ = InitializeAsync();

        _logger.LogInformation("MainViewModel Initializing Complete");
    }

    private void UpdateUnreadCount()
    {
        UnreadNotificationCount = _notificationService.UnreadNotifications.Count;
    }

    private void OnProjectChanged(Guid projectId)
    {
        _logger.LogInformation("Project changed event received for {ProjectId}", projectId);

        _ = _dispatcherService.InvokeAsync(async () =>
        {
            _saveCoordinator.IsEnabled = false;
            try
            {
                var updatedProjectEntity = await _findProjectUseCase.ExecuteAsync(projectId);
                if (updatedProjectEntity == null) return;

                var existingViewModel = Projects.FirstOrDefault(pvm => pvm.Id == projectId);
                if (existingViewModel != null)
                {
                    _logger.LogDebug("Updating existing project {ProjectId} ViewModel via differential sync.", projectId);
                    existingViewModel.UpdateFromModel(updatedProjectEntity);
                }
                else
                {
                    _logger.LogDebug("Adding new project {ProjectId} ViewModel from sync.", projectId);
                    var newProjectViewModel = new ProjectViewModel(updatedProjectEntity);
                    Projects.Add(newProjectViewModel);
                }

                // 同期後に期限チェックを実行
                _checkDeadlinesUseCase.Execute(Projects.Select(p => p.Model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project {ProjectId} on change event.", projectId);
            }
            finally
            {
                _saveCoordinator.IsEnabled = true;
            }
        });
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        _logger.LogInformation("Loading initial projects");
        _saveCoordinator.IsEnabled = false;
        try
        {
            var projectEntities = await _loadUseCase.ExecuteAsync();
            foreach (var projectEntity in projectEntities)
            {
                var projectViewModel = new ProjectViewModel(projectEntity);
                Projects.Add(projectViewModel);
            }

            // 初期化後に期限チェックを実行
            _checkDeadlinesUseCase.Execute(Projects.Select(p => p.Model));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize projects");
        }
        finally
        {
            _saveCoordinator.IsEnabled = true;
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
