using System;

namespace TimeLeaf.Services;

/// <summary>
/// OS 標準の通知機能（Toast 通知など）を提供するサービス。
/// システムトレイ（通知領域）への常駐管理も担当します。
/// </summary>
public interface IOSNotificationService
{
    /// <summary>
    /// OS 標準の通知を表示します。
    /// </summary>
    /// <param name="title">通知のタイトル。</param>
    /// <param name="message">通知の内容。</param>
    void Show(string title, string message);

    /// <summary>
    /// トレイアイコンからアプリケーションの表示が要求されたときに発生します。
    /// </summary>
    event EventHandler RequestOpen;

    /// <summary>
    /// トレイアイコンからアプリケーションの終了が要求されたときに発生します。
    /// </summary>
    event EventHandler RequestExit;

    /// <summary>
    /// トレイアイコンの可視状態を設定します。
    /// </summary>
    /// <param name="isVisible">表示する場合は true。</param>
    void SetTrayVisible(bool isVisible);
}
