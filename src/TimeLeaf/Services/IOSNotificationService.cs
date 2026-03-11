using System;

namespace TimeLeaf.Services;

/// <summary>
/// OS 標準の通知機能（Toast 通知など）を提供するサービス。
/// </summary>
public interface IOSNotificationService
{
    /// <summary>
    /// OS 標準の通知を表示します。
    /// </summary>
    /// <param name="title">通知のタイトル。</param>
    /// <param name="message">通知の内容。</param>
    void Show(string title, string message);
}
