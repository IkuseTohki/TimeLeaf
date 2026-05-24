using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
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
    private readonly IProjectService _projectService;
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly IProjectSaveCoordinator _saveCoordinator;
    private readonly IDispatcherService _dispatcherService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly INotificationService _notificationService;
    private readonly ISnackbarService _snackbarService;
    private readonly IOSNotificationService _osNotificationService;
    private readonly ICheckTaskDeadlinesUseCase _checkDeadlinesUseCase;
    private readonly IDialogService _dialogService;
    private readonly IIdentityService _identityService;
    private readonly IApplicationSettingsRepository _settingsRepo;
    private readonly ApplicationSettings _settings;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private MainNavigationContext _navigationContext;

    partial void OnNavigationContextChanged(MainNavigationContext value)
    {
        _logger.LogInformation("NavigationContext changed to {Context}", value);

        // 既存の ViewModel を破棄（IDisposable の場合）
        if (CurrentViewModel is IDisposable disposable)
        {
            _logger.LogDebug("Disposing old ViewModel: {ViewModelType}", CurrentViewModel.GetType().Name);
            disposable.Dispose();
        }

        switch (value)
        {
            case MainNavigationContext.Home:
                CurrentViewModel = _viewModelFactory.CreateHomeViewModel(Projects);
                break;
            case MainNavigationContext.AllTasks:
                var allTasksVm = _viewModelFactory.CreateAllTasksViewModel(Projects);
                allTasksVm.RequestNavigation += (s, p) => NavigateToProject(p.Project, p.Task);
                CurrentViewModel = allTasksVm;
                break;
            case MainNavigationContext.Notifications:
                var notificationsVm = _viewModelFactory.CreateNotificationsViewModel(Projects);
                notificationsVm.RequestNavigation += (s, p) => NavigateToProject(p.Project, p.Task);
                CurrentViewModel = notificationsVm;
                break;
            case MainNavigationContext.ProjectDetail:
                // ProjectDetail は NavigateToProject 経由で設定される
                break;
        }
    }

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    /// <summary>
    /// 現在のユーザー名。
    /// </summary>
    public string CurrentUserName => _identityService.CurrentUserId == Guid.Empty ? "Guest" : Environment.UserName; // 仮実装

    [RelayCommand]
    private async System.Threading.Tasks.Task EditProfile()
    {
        _logger.LogInformation("EditProfile started.");
        try
        {
            var profileVm = new ProfileEditViewModel(_identityService);
            await profileVm.LoadAsync();
            var result = await _dialogService.ShowDialogAsync(profileVm);

            if (result)
            {
                OnPropertyChanged(nameof(CurrentUserName));
                _logger.LogInformation("Profile updated successfully.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit profile.");
        }
    }

    /// <summary>
    /// サイドバーが展開されているかどうか。
    /// </summary>
    [ObservableProperty]
    private bool _isSidebarExpanded = true;

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
    [ObservableProperty]
    private bool _canExit;

    private bool _forceExit = false;

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
        IProjectService projectService,
        IAddProjectUseCase addProjectUseCase,
        IProjectSaveCoordinator saveCoordinator,
        IDispatcherService dispatcherService,
        IViewModelFactory viewModelFactory,
        INotificationService notificationService,
        ISnackbarService snackbarService,
        IOSNotificationService osNotificationService,
        ICheckTaskDeadlinesUseCase checkDeadlinesUseCase,
        IDialogService dialogService,
        IIdentityService identityService,
        IApplicationSettingsRepository settingsRepo,
        ApplicationSettings settings,
        ILogger<MainViewModel> logger
    )
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _findProjectUseCase = findProjectUseCase;
        _projectService = projectService;
        _addProjectUseCase = addProjectUseCase;
        _saveCoordinator = saveCoordinator;
        _dispatcherService = dispatcherService;
        _viewModelFactory = viewModelFactory;
        _notificationService = notificationService;
        _snackbarService = snackbarService;
        _osNotificationService = osNotificationService;
        _checkDeadlinesUseCase = checkDeadlinesUseCase;
        _dialogService = dialogService;
        _identityService = identityService;
        _settingsRepo = settingsRepo;
        _settings = settings;
        _logger = logger;

        _navigationContext = MainNavigationContext.Home;
        _currentViewModel = _viewModelFactory.CreateHomeViewModel(Projects);

        _logger.LogInformation("MainViewModel Initializing");

        // 外部変更および内部状態更新の監視をサービス経由で行う
        _projectService.ProjectAdded += OnProjectAdded;
        _projectService.ProjectUpdated += OnProjectUpdated;
        _projectService.ProjectRemoved += OnProjectRemoved;

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
            _forceExit = true;
            UpdateCanExit();
            ExitRequested?.Invoke(this, EventArgs.Empty);
        };

        // 設定変更の購読
        _settingsRepo.SettingsChanged += (s, e) => UpdateCanExit();

        // 初期状態の設定
        UpdateCanExit();

        // 自動保存の開始
        _saveCoordinator.StartMonitoring(Projects);

        _ = InitializeAsync();

        _logger.LogInformation("MainViewModel Initializing Complete");
    }

    private void UpdateCanExit()
    {
        CanExit = _forceExit || !_settings.MinimizeOnClose;
    }

    private void UpdateUnreadCount()
    {
        UnreadNotificationCount = _notificationService.UnreadNotifications.Count;
    }

    private void OnProjectAdded(Project project)
    {
        _ = _dispatcherService.InvokeAsync(() =>
        {
            if (!Projects.Any(p => p.Id == project.Id))
            {
                var vm = _viewModelFactory.CreateProjectViewModel(project);
                Projects.Add(vm);
            }
        });
    }

    private void OnProjectUpdated(Project project)
    {
        _logger.LogInformation("Project updated event received for {ProjectId}", project.Id);

        _ = _dispatcherService.InvokeAsync(() =>
        {
            _saveCoordinator.IsEnabled = false;
            try
            {
                var existingViewModel = Projects.FirstOrDefault(pvm => pvm.Id == project.Id);
                if (existingViewModel != null)
                {
                    _logger.LogDebug(
                        "Updating existing project {ProjectId} ViewModel via differential sync.",
                        project.Id
                    );
                    existingViewModel.UpdateFromModel(project);
                }
                else
                {
                    // まだリストにない場合は追加
                    var newProjectViewModel = _viewModelFactory.CreateProjectViewModel(project);
                    Projects.Add(newProjectViewModel);
                }

                // 更新後に期限チェックを実行
                _checkDeadlinesUseCase.Execute(Projects.Select(p => p.Model));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project {ProjectId} on change event.", project.Id);
            }
            finally
            {
                _saveCoordinator.IsEnabled = true;
            }
        });
    }

    private void OnProjectRemoved(Guid projectId)
    {
        _ = _dispatcherService.InvokeAsync(() =>
        {
            var vm = Projects.FirstOrDefault(p => p.Id == projectId);
            if (vm != null)
            {
                Projects.Remove(vm);
            }
        });
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        _logger.LogInformation("Loading initial projects via ProjectService");
        _saveCoordinator.IsEnabled = false;
        try
        {
            // ユースケース経由でサービスのロードを叩く
            var projectEntities = await _loadUseCase.ExecuteAsync();

            await _dispatcherService.InvokeAsync(() =>
            {
                Projects.Clear();
                foreach (var projectEntity in projectEntities)
                {
                    var projectViewModel = _viewModelFactory.CreateProjectViewModel(projectEntity);
                    Projects.Add(projectViewModel);
                }
            });

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
    private void NavigateToHome()
    {
        NavigationContext = MainNavigationContext.Home;
    }

    [RelayCommand]
    private void NavigateToAllTasks()
    {
        NavigationContext = MainNavigationContext.AllTasks;
    }

    [RelayCommand]
    private void NavigateToNotifications()
    {
        NavigationContext = MainNavigationContext.Notifications;
    }

    [RelayCommand]
    private void NavigateToProject(ProjectViewModel projectViewModel)
    {
        NavigateToProject(projectViewModel, null);
    }

    /// <summary>
    /// 指定されたプロジェクト（およびオプションでタスク）へ遷移します。
    /// </summary>
    /// <param name="projectViewModel">遷移先プロジェクト。</param>
    /// <param name="task">遷移時に直接開くタスク（オプション）。</param>
    /// <summary>
    /// 指定されたプロジェクト（およびオプションでタスク）へ遷移します。
    /// </summary>
    /// <param name="projectViewModel">遷移先プロジェクト。</param>
    /// <param name="task">遷移時に直接開くタスク（オプション）。</param>
    public void NavigateToProject(ProjectViewModel projectViewModel, ProjectTaskViewModel? task)
    {
        if (projectViewModel == null)
            return;
        _logger.LogInformation("Navigating to project {ProjectId} (Task: {TaskId})", projectViewModel.Id, task?.Id);

        // 既存の ViewModel を破棄（IDisposable の場合）
        // NavigationContext が変わらない場合、OnNavigationContextChanged が呼ばれないため、ここで明示的に行う
        if (CurrentViewModel is IDisposable disposable)
        {
            _logger.LogDebug(
                "Disposing old ViewModel before navigating to project: {ViewModelType}",
                CurrentViewModel.GetType().Name
            );
            disposable.Dispose();
        }

        // Context を先にセットし、その後に ViewModel をセットする
        NavigationContext = MainNavigationContext.ProjectDetail;
        var workspaceVm = _viewModelFactory.CreateProjectWorkspaceViewModel(projectViewModel, Projects);
        workspaceVm.ProjectNavigationRequested += (s, p) => NavigateToProject(p.Project, p.Task);

        if (task != null)
        {
            workspaceVm.OpenTaskDetail(task);
        }

        CurrentViewModel = workspaceVm;
    }

    [RelayCommand]
    private void NavigateBack()
    {
        _logger.LogInformation("Navigating back to Home");
        NavigationContext = MainNavigationContext.Home;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    /// <summary>
    /// プロジェクト作成ダイアログを表示し、新規プロジェクトを追加します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task AddProject()
    {
        _logger.LogInformation("AddProject started from MainViewModel.");

        try
        {
            var addProjectVm = _viewModelFactory.CreateAddProjectViewModel();
            var result = await _dialogService.ShowDialogAsync(addProjectVm);

            if (result)
            {
                _logger.LogDebug("Adding project: {Name}", addProjectVm.Name);

                // ユースケースを実行（内部で ProjectService.SaveProjectAsync が呼ばれ、イベントが飛んでくる）
                await _addProjectUseCase.ExecuteAsync(addProjectVm.Name, addProjectVm.Description, addProjectVm.Status);

                _logger.LogInformation("AddProject execution requested.");

                // Note: Projects コレクションへの追加は OnProjectAdded イベントによって自動で行われるため、
                // ここでの明示的な Projects.Add は不要になります。
                // 遷移が必要な場合は、追加された VM を探すか、イベント経由で通知を受け取る仕組みが必要ですが、
                // 一旦はイベントによる自動追加を優先します。
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add project from sidebar.");
        }
    }
}
