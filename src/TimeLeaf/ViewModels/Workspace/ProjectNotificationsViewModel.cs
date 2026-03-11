using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// プロジェクト内の通知一覧を表示・管理するための ViewModel。
/// </summary>
public partial class ProjectNotificationsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

    /// <summary>
    /// 未読通知のリスト。
    /// </summary>
    public ObservableCollection<Notification> UnreadNotifications { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public ProjectNotificationsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

        // 初期ロード
        RefreshNotifications();

        // 通知サービスからの変更を監視
        _notificationService.UnreadCountChanged += (s, e) => RefreshNotifications();
    }

    private void RefreshNotifications()
    {
        UnreadNotifications.Clear();
        foreach (var notification in _notificationService.UnreadNotifications)
        {
            UnreadNotifications.Add(notification);
        }
    }

    /// <summary>
    /// 指定された通知を既読としてマークします。
    /// </summary>
    [RelayCommand]
    private void MarkAsRead(Notification notification)
    {
        if (notification == null) return;
        _notificationService.MarkAsRead(notification.Id);
    }

    /// <summary>
    /// すべての通知を既読としてマークします。
    /// </summary>
    [RelayCommand]
    private void MarkAllAsRead()
    {
        _notificationService.MarkAllAsRead();
    }
}
