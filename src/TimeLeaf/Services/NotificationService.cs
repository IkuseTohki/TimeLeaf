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
        // 重複チェック: 同じIDを持つ未読通知、あるいは同じ関連エンティティに対する同じタイトルの未読通知が既に存在する場合は追加しない
        if (_notifications.Any(n => !n.IsRead && (n.Id == notification.Id || (n.Title == notification.Title && n.RelatedEntityId == notification.RelatedEntityId))))
        {
            return;
        }

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
