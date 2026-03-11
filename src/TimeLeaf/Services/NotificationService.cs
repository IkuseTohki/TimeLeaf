using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーション全体の通知を管理するサービスの実装。
/// </summary>
public class NotificationService : INotificationService
{
    private readonly List<Notification> _notifications = new();

    public IReadOnlyList<Notification> UnreadNotifications => _notifications.Where(n => !n.IsRead).ToList();

    public event EventHandler? UnreadCountChanged;

    public event EventHandler<Notification>? NotificationAdded;

    public void Notify(Notification notification)
    {
        _notifications.Add(notification);
        NotificationAdded?.Invoke(this, notification);
        OnUnreadCountChanged();
    }

    public void MarkAsRead(Guid notificationId)
    {
        var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification != null && !notification.IsRead)
        {
            notification.MarkAsRead();
            OnUnreadCountChanged();
        }
    }

    public void MarkAllAsRead()
    {
        var unread = _notifications.Where(n => !n.IsRead).ToList();
        if (unread.Any())
        {
            foreach (var n in unread)
            {
                n.MarkAsRead();
            }
            OnUnreadCountChanged();
        }
    }

    protected virtual void OnUnreadCountChanged()
    {
        UnreadCountChanged?.Invoke(this, EventArgs.Empty);
    }
}
