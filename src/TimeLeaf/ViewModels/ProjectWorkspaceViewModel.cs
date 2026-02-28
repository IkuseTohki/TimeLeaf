using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 特定のプロジェクト内のナビゲーションと各サブビューの管理を担当する親ViewModel。
/// </summary>
public partial class ProjectWorkspaceViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly IViewModelFactory _viewModelFactory;
    private readonly ILogger<ProjectWorkspaceViewModel> _logger;

    /// <summary>
    /// 現在表示中のサブビューのViewModel。
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentSubViewModel;

    /// <summary>
    /// サイドバーが展開されているかどうか。
    /// </summary>
    [ObservableProperty]
    private bool _isSidebarExpanded = true;

    /// <summary>
    /// 管理対象プロジェクトの名称。
    /// </summary>
    public string ProjectName => _projectViewModel.Name;

    /// <summary>
    /// 管理対象プロジェクトのID。
    /// </summary>
    public Guid Id => _projectViewModel.Id;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public ProjectWorkspaceViewModel(
        ProjectViewModel projectViewModel,
        IViewModelFactory viewModelFactory,
        ILogger<ProjectWorkspaceViewModel> logger)
    {
        _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
        _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        _logger = logger;

        // 初期表示としてダッシュボードを設定
        _currentSubViewModel = _viewModelFactory.CreateProjectDashboardViewModel(_projectViewModel);

        _logger.LogInformation("ProjectWorkspaceViewModel initialized for project {ProjectId}.", _projectViewModel.Id);
    }

    /// <summary>
    /// サイドバーの開閉を切り替えます。
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar() => IsSidebarExpanded = !IsSidebarExpanded;

    /// <summary>
    /// 表示するサブビューを切り替えます。
    /// </summary>
    /// <param name="viewName">切り替え先のビュー名 (Dashboard, Tasks, Timeline, Settings)</param>
    [RelayCommand]
    private void SwitchSubView(string viewName)
    {
        _logger.LogInformation("Switching sub-view to {ViewName}", viewName);

        CurrentSubViewModel = viewName switch
        {
            "Dashboard" => _viewModelFactory.CreateProjectDashboardViewModel(_projectViewModel),
            "Tasks" => _viewModelFactory.CreateProjectTasksViewModel(_projectViewModel),
            "Timeline" => _viewModelFactory.CreateProjectTimelineViewModel(_projectViewModel),
            "Settings" => _viewModelFactory.CreateProjectSettingsViewModel(_projectViewModel),
            _ => CurrentSubViewModel
        };
    }
}
