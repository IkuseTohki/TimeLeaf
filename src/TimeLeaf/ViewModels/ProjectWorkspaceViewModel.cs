using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のナビゲーションと各サブビューの管理を担当する親ViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly System.Collections.ObjectModel.ObservableCollection<ProjectViewModel> _projects;
    private readonly INotificationService _notificationService;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<ProjectWorkspaceViewModel> _logger;

    /// <summary>
    /// 現在表示中のサブビューの名前。
    /// </summary>
    [ObservableProperty]
    private string _currentViewName = "Dashboard";

    partial void OnCurrentViewNameChanged(string value)
    {
        // プロパティが変更されたら、自動的にビュー切り替えロジックを実行
        if (CurrentSubViewModel?.GetType().Name.Contains(value) != true)
        {
            SwitchSubView(value);
        }
    }

    /// <summary>
    /// 現在表示中のサブビューのViewModel。
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentSubViewModel;

    /// <summary>
    /// 未読の通知件数。
    /// </summary>
    [ObservableProperty]
    private int _unreadNotificationCount;

    /// <summary>
    /// ナビゲーション用の項目リスト。
    /// </summary>
    public List<NavigationItem> NavigationItems { get; } =
        new()
        {
            new NavigationItem("Dashboard", "Dashboard"),
            new NavigationItem("Tasks", "Tasks"),
            new NavigationItem("Timeline", "Timeline"),
            new NavigationItem("Notifications", "Notifications"),
            new NavigationItem("Settings", "Settings"),
        };

    /// <summary>
    /// 管理対象プロジェクトの ViewModel。
    /// </summary>
    public ProjectViewModel ProjectViewModel => _projectViewModel;

    /// <summary>
    /// 管理対象プロジェクトの名称。
    /// </summary>
    public string ProjectName => _projectViewModel.Name;

    /// <summary>
    /// 管理対象プロジェクトのID。
    /// </summary>
    public Guid Id => _projectViewModel.Id;

    /// <summary>
    /// ユーザーがプロジェクトにアサインされているか。
    /// </summary>
    public bool IsUserAssigned => _projectViewModel.IsAssignedToMe;

    /// <summary>
    /// 特定のプロジェクト（およびオプションでタスク）への遷移が要求されたときに発生します。
    /// </summary>
    public event EventHandler<(ProjectViewModel Project, ProjectTaskViewModel? Task)>? ProjectNavigationRequested;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public ProjectWorkspaceViewModel(
        ProjectViewModel projectViewModel,
        System.Collections.ObjectModel.ObservableCollection<ProjectViewModel> projects,
        INotificationService notificationService,
        IViewModelFactory viewModelFactory,
        ICheckAssignmentUseCase checkAssignment,
        ILogger<ProjectWorkspaceViewModel> logger
    )
    {
        _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // ProjectViewModel の変更（アサイン状態）を監視
        _projectViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.IsAssignedToMe))
            {
                OnPropertyChanged(nameof(IsUserAssigned));
            }
        };

        // 初期表示としてダッシュボードを設定
        _currentSubViewModel = _viewModelFactory.CreateProjectDashboardViewModel(_projectViewModel);

        // 通知カウントの同期
        _notificationService.UnreadCountChanged += (s, e) => UpdateUnreadCount();
        UpdateUnreadCount();

        _logger.LogInformation("ProjectWorkspaceViewModel initialized for project {ProjectId}.", _projectViewModel.Id);
    }

    private void UpdateUnreadCount()
    {
        // プロジェクトに関連する通知のみをカウント
        var project = _projectViewModel;
        var taskIds = project.Tasks.Select(t => t.Id.ToString()).ToList();
        var filterStr = project.Id.ToString();

        UnreadNotificationCount = _notificationService.UnreadNotifications.Count(n =>
            n.RelatedEntityId == filterStr || (n.RelatedEntityId != null && taskIds.Contains(n.RelatedEntityId))
        );
    }

    /// <summary>
    /// 表示するサブビューを切り替えます。
    /// </summary>
    /// <param name="viewName">切り替え先のビュー名 (Dashboard, Tasks, Timeline, Settings)</param>
    [RelayCommand]
    private void SwitchSubView(string viewName)
    {
        _logger.LogInformation("Switching sub-view to {ViewName}", viewName);

        // 古いViewModelのイベント購読解除
        if (CurrentSubViewModel is ProjectTasksViewModel oldTasksVm)
        {
            oldTasksVm.TaskDetailRequested -= OnTaskDetailRequested;
        }

        CurrentViewName = viewName;
        CurrentSubViewModel = viewName switch
        {
            "Dashboard" => _viewModelFactory.CreateProjectDashboardViewModel(_projectViewModel),
            "Tasks" => CreateTasksViewModel(),
            "Timeline" => _viewModelFactory.CreateProjectTimelineViewModel(_projectViewModel),
            "Settings" => _viewModelFactory.CreateProjectSettingsViewModel(_projectViewModel),
            "Notifications" => CreateNotificationsViewModel(),
            _ => CurrentSubViewModel,
        };
    }

    private NotificationsViewModel CreateNotificationsViewModel()
    {
        var vm = _viewModelFactory.CreateNotificationsViewModel(_projects, _projectViewModel.Id);
        vm.RequestNavigation += (s, p) => ProjectNavigationRequested?.Invoke(this, p);
        return vm;
    }

    private ProjectTasksViewModel CreateTasksViewModel()
    {
        var vm = _viewModelFactory.CreateProjectTasksViewModel(_projectViewModel);
        if (vm != null)
        {
            vm.TaskDetailRequested += OnTaskDetailRequested;
        }
        return vm!;
    }

    private void OnTaskDetailRequested(object? sender, ProjectTaskViewModel task)
    {
        OpenTaskDetail(task);
    }

    /// <summary>
    /// 指定されたタスクの詳細を表示します。
    /// </summary>
    /// <param name="task">表示対象のタスクViewModel。</param>
    [RelayCommand]
    public void OpenTaskDetail(ProjectTaskViewModel task)
    {
        _logger.LogInformation("Navigating to task detail for {TaskName} within main content area.", task.Name);
        var detailVm = _viewModelFactory.CreateTaskDetailViewModel(_projectViewModel, task);

        // 閉じる要求（戻る要求）をハンドル
        detailVm.RequestClose += (result) => CloseTaskDetail();

        CurrentViewName = "Tasks";
        CurrentSubViewModel = detailVm;
    }

    /// <summary>
    /// タスク詳細の表示を閉じ、タスク一覧に戻ります。
    /// </summary>
    [RelayCommand]
    private void CloseTaskDetail()
    {
        _logger.LogInformation("Closing task detail and returning to task list.");
        SwitchSubView("Tasks");
    }
}

/// <summary>
/// ナビゲーション項目を表すクラス。
/// </summary>
public record NavigationItem(string Label, string Parameter);
