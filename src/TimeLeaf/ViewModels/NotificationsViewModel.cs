using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 通知一覧を表示・管理するための ViewModel。
/// </summary>
public partial class NotificationsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly ObservableCollection<ProjectViewModel> _projects;
    private readonly Guid? _projectIdFilter;
    private readonly ILogger<NotificationsViewModel> _logger;

    /// <summary>
    /// 未読通知のリスト。
    /// </summary>
    public ObservableCollection<NotificationViewModel> UnreadNotifications { get; } = new();

    /// <summary>
    /// 特定のプロジェクト（およびオプションでタスク）への遷移が要求されたときに発生します。
    /// </summary>
    public event EventHandler<(ProjectViewModel Project, ProjectTaskViewModel? Task)>? RequestNavigation;

    /// <summary>
    /// グローバルコンテキスト（プロジェクトフィルターなし）で表示されているかどうか。
    /// </summary>
    public bool IsGlobalContext => !_projectIdFilter.HasValue;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="notificationService">通知サービス。</param>
    /// <param name="projects">全プロジェクトのリスト。</param>
    /// <param name="logger">ロガー。</param>
    /// <param name="projectIdFilter">特定のプロジェクトのみを表示する場合のIDフィルタ（オプション）。</param>
    public NotificationsViewModel(
        INotificationService notificationService,
        ObservableCollection<ProjectViewModel> projects,
        ILogger<NotificationsViewModel> logger,
        Guid? projectIdFilter = null
    )
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectIdFilter = projectIdFilter;

        // 初期ロード
        RefreshNotifications();

        // 通知サービスからの変更を監視
        _notificationService.UnreadCountChanged += (s, e) => RefreshNotifications();
    }

    private void RefreshNotifications()
    {
        UnreadNotifications.Clear();

        var query = _notificationService.UnreadNotifications.AsEnumerable();

        // フィルターが指定されている場合は適用
        if (_projectIdFilter.HasValue)
        {
            var project = _projects.FirstOrDefault(p => p.Id == _projectIdFilter.Value);
            var taskIds = project?.Tasks.Select(t => t.Id.ToString()).ToList() ?? new List<string>();
            var filterStr = _projectIdFilter.Value.ToString();

            query = query.Where(n =>
                n.RelatedEntityId == filterStr || (n.RelatedEntityId != null && taskIds.Contains(n.RelatedEntityId))
            );
        }

        foreach (var notification in query)
        {
            UnreadNotifications.Add(
                new NotificationViewModel(notification, _notificationService, OnNotificationNavigate)
            );
        }
    }

    private void OnNotificationNavigate(NotificationViewModel vm)
    {
        if (vm.RelatedEntityId == null)
        {
            _logger.LogWarning("Notification {NotificationId} has no RelatedEntityId.", vm.Id);
            return;
        }

        _logger.LogInformation(
            "Navigation requested for notification {NotificationId} with RelatedEntityId {RelatedEntityId}",
            vm.Id,
            vm.RelatedEntityId
        );

        if (Guid.TryParse(vm.RelatedEntityId, out var entityId))
        {
            // 1. プロジェクトIDとして検索
            var projectById = _projects.FirstOrDefault(p => p.Id == entityId);
            if (projectById != null)
            {
                _logger.LogInformation("Found related project {ProjectId} by ID. Raising RequestNavigation.", entityId);
                RequestNavigation?.Invoke(this, (projectById, null));
                return;
            }

            // 2. タスクIDとして全プロジェクト内を検索
            foreach (var project in _projects)
            {
                var task = project.Tasks.FirstOrDefault(t => t.Id == entityId);
                if (task != null)
                {
                    _logger.LogInformation(
                        "Found related task {TaskId} in project {ProjectId}. Raising RequestNavigation.",
                        entityId,
                        project.Id
                    );
                    RequestNavigation?.Invoke(this, (project, task));
                    return;
                }
            }

            _logger.LogWarning("Entity {EntityId} not found in current projects or tasks.", entityId);
        }
        else
        {
            _logger.LogWarning("Failed to parse RelatedEntityId {RelatedEntityId} as Guid.", vm.RelatedEntityId);
        }
    }

    /// <summary>
    /// すべての通知を既読としてマークします。
    /// </summary>
    [RelayCommand]
    private void MarkAllAsRead()
    {
        // フィルターがある場合は、そのプロジェクトの分だけ既読にする
        if (_projectIdFilter.HasValue)
        {
            foreach (var vm in UnreadNotifications.ToList())
            {
                vm.MarkAsReadCommand.Execute(null);
            }
        }
        else
        {
            _notificationService.MarkAllAsRead();
        }
    }
}
