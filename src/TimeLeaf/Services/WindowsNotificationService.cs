using System;
using System.Drawing;
using System.Windows.Forms;

namespace TimeLeaf.Services;

/// <summary>
/// Windows OS 標準の通知機能を提供するサービスの実装。
/// NotifyIcon を使用してバルーン通知（Windows 10以降は Toast）を表示します。
/// </summary>
public class WindowsNotificationService : IOSNotificationService, IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private bool _isDisposed;

    /// <summary>
    /// トレイアイコンからアプリケーションの表示が要求されたときに発生します。
    /// </summary>
    public event EventHandler? RequestOpen;

    /// <summary>
    /// トレイアイコンからアプリケーションの終了が要求されたときに発生します。
    /// </summary>
    public event EventHandler? RequestExit;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    public WindowsNotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            // デフォルトのアイコン（システムアイコン）を設定
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "TimeLeaf",
        };

        // ダブルクリックで開く
        _notifyIcon.DoubleClick += (s, e) => RequestOpen?.Invoke(this, EventArgs.Empty);

        // コンテキストメニューの作成
        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("TimeLeaf を開く", null, (s, e) => RequestOpen?.Invoke(this, EventArgs.Empty));
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("終了", null, (s, e) => RequestExit?.Invoke(this, EventArgs.Empty));

        _notifyIcon.ContextMenuStrip = contextMenu;
    }

    /// <summary>
    /// 通知を表示します。
    /// </summary>
    /// <param name="title">通知のタイトル。</param>
    /// <param name="message">通知の内容。</param>
    public void Show(string title, string message)
    {
        if (_isDisposed)
            return;

        // 5000ミリ秒（5秒）表示
        _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
    }

    /// <summary>
    /// トレイアイコンの可視状態を設定します。
    /// </summary>
    /// <param name="isVisible">表示する場合は true。</param>
    public void SetTrayVisible(bool isVisible)
    {
        if (_isDisposed)
            return;
        _notifyIcon.Visible = isVisible;
    }

    /// <summary>
    /// リソースを破棄します。
    /// </summary>
    public void Dispose()
    {
        if (!_isDisposed)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// デストラクタ。
    /// </summary>
    ~WindowsNotificationService()
    {
        Dispose();
    }
}
