using System;
using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーション全体の通知を管理するサービス。
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 未読の通知リストを取得します。
    /// </summary>
    IReadOnlyList<Notification> UnreadNotifications { get; }

    /// <summary>
    /// 未読の通知件数が変更されたときに発生します。
    /// </summary>
    event EventHandler UnreadCountChanged;

    /// <summary>
    /// 新しい通知が追加されたときに発生します。
    /// </summary>
    event EventHandler<Notification> NotificationAdded;

    /// <summary>
    /// 新しい通知を登録します。
    /// </summary>
    /// <param name="notification">登録する通知オブジェクト。</param>
    void Notify(Notification notification);

    /// <summary>
    /// 指定された通知を既読としてマークします。
    /// </summary>
    /// <param name="notificationId">既読にする通知のID。</param>
    void MarkAsRead(Guid notificationId);

    /// <summary>
    /// すべての通知を既読としてマークします。
    /// </summary>
    void MarkAllAsRead();
}
