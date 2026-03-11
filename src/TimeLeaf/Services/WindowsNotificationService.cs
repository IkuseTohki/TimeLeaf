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
    /// コンストラクタ。
    /// </summary>
    public WindowsNotificationService()
    {
        _notifyIcon = new NotifyIcon
        {
            // デフォルトのアイコン（システムアイコン）を設定
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "TimeLeaf"
        };
    }

    /// <summary>
    /// 通知を表示します。
    /// </summary>
    /// <param name="title">通知のタイトル。</param>
    /// <param name="message">通知の内容。</param>
    public void Show(string title, string message)
    {
        if (_isDisposed) return;

        // 5000ミリ秒（5秒）表示
        _notifyIcon.ShowBalloonTip(5000, title, message, ToolTipIcon.Info);
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
