using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// アプリケーション内の通知イベントを表すエンティティ。
/// </summary>
public class Notification
{
    /// <summary>
    /// 通知の一意なID。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 通知のタイトル。
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// 通知の内容。
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// 通知に関連するエンティティのID（タスクIDなど、オプション）。
    /// </summary>
    public string? RelatedEntityId { get; }

    /// <summary>
    /// 通知の発生時刻。
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// 通知を既読したかどうか。
    /// </summary>
    public bool IsRead { get; private set; }

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="title">通知のタイトル。</param>
    /// <param name="message">通知の内容。</param>
    /// <param name="relatedEntityId">関連エンティティのID。</param>
    /// <param name="id">通知の一意なID。指定しない場合は自動生成されます。</param>
    public Notification(string title, string message, string? relatedEntityId = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Title = title;
        Message = message;
        RelatedEntityId = relatedEntityId;
        CreatedAt = DateTime.Now;
        IsRead = false;
    }

    /// <summary>
    /// 通知を既読としてマークします。
    /// </summary>
    public void MarkAsRead()
    {
        IsRead = true;
    }
}
