using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 個別の通知項目を表示するための ViewModel。
/// エンティティをラップし、既読化や遷移などのロジックを提供する。
/// </summary>
public partial class NotificationViewModel : ObservableObject
{
    private readonly Notification _entity;
    private readonly INotificationService _notificationService;
    private readonly Action<NotificationViewModel> _onNavigate;

    /// <summary>
    /// 通知の一意なID。
    /// </summary>
    public Guid Id => _entity.Id;

    /// <summary>
    /// 通知のタイトル。
    /// </summary>
    public string Title => _entity.Title;

    /// <summary>
    /// 通知の内容。
    /// </summary>
    public string Message => _entity.Message;

    /// <summary>
    /// 通知に関連するエンティティのID（オプション）。
    /// </summary>
    public string? RelatedEntityId => _entity.RelatedEntityId;

    /// <summary>
    /// 通知の発生時刻。
    /// </summary>
    public DateTime CreatedAt => _entity.CreatedAt;

    /// <summary>
    /// 関連エンティティへの遷移が可能かどうか。
    /// </summary>
    public bool CanNavigate => !string.IsNullOrEmpty(RelatedEntityId);

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="entity">ラップする通知エンティティ。</param>
    /// <param name="notificationService">既読化などの操作に使用する通知サービス。</param>
    /// <param name="onNavigate">関連エンティティへの遷移が要求された際のコールバック。</param>
    public NotificationViewModel(
        Notification entity,
        INotificationService notificationService,
        Action<NotificationViewModel> onNavigate)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _onNavigate = onNavigate ?? throw new ArgumentNullException(nameof(onNavigate));
    }

    /// <summary>
    /// この通知を既読としてマークします。
    /// </summary>
    [RelayCommand]
    private void MarkAsRead()
    {
        _notificationService.MarkAsRead(Id);
    }

    /// <summary>
    /// 関連するエンティティ（プロジェクトやタスク）へ遷移します。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void Navigate()
    {
        _onNavigate(this);
    }
}
