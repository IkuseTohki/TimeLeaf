using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Services;

[TestClass]
public class NotificationServiceTests
{
    private NotificationService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new NotificationService();
    }

    /// <summary>
    /// テスト観点: 重複でない通知が正しく追加され、未読リストに含まれることを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_ShouldAddNotification_WhenNotDuplicate()
    {
        // Arrange
        var n = new Notification("Title", "Message");

        // Act
        _service.Notify(n);

        // Assert
        Assert.AreEqual(1, _service.UnreadNotifications.Count);
        Assert.AreEqual(n.Id, _service.UnreadNotifications[0].Id);
    }

    /// <summary>
    /// テスト観点: 同じ関連エンティティとタイトルを持つ通知が複数回送られた場合、重複して追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_ShouldNotAddDuplicateNotification()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var title = "タスク期限切れ";

        var n1 = new Notification(title, "1回目", taskId);
        var n2 = new Notification(title, "2回目", taskId);

        // Act
        _service.Notify(n1);
        _service.Notify(n2);

        // Assert
        Assert.AreEqual(1, _service.UnreadNotifications.Count);
    }

    /// <summary>
    /// テスト観点: 明示的に同じIDを持つ通知が複数回送られた場合、重複して追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_ShouldNotAddDuplicateId()
    {
        // Arrange
        var id = Guid.NewGuid();
        var n1 = new Notification("T1", "M1", null, id);
        var n2 = new Notification("T2", "M2", null, id);

        // Act
        _service.Notify(n1);
        _service.Notify(n2);

        // Assert
        Assert.AreEqual(1, _service.UnreadNotifications.Count);
    }

    /// <summary>
    /// テスト観点: 既存の通知が「既読」である場合、同じ内容の通知が送られたら重複とみなし追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_DisallowSameNotification_IfExistingIsRead()
    {
        // Arrange
        var taskId = Guid.NewGuid().ToString();
        var title = "タスク期限切れ";
        var n1 = new Notification(title, "1回目", taskId);
        _service.Notify(n1);
        _service.MarkAsRead(n1.Id); // 既読にする

        var n2 = new Notification(title, "2回目", taskId);

        // Act
        _service.Notify(n2);

        // Assert
        Assert.AreEqual(0, _service.UnreadNotifications.Count, "新しい未読通知が1つあるべき");
    }

    /// <summary>
    /// テスト観点: 通知が正しく追加された際、NotificationAdded と UnreadCountChanged イベントが発生することを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_ShouldRaiseEvents_WhenNewNotificationAdded()
    {
        // Arrange
        var addedRaised = false;
        var countChangedRaised = false;
        _service.NotificationAdded += (s, e) => addedRaised = true;
        _service.UnreadCountChanged += (s, e) => countChangedRaised = true;

        // Act
        _service.Notify(new Notification("T", "M"));

        // Assert
        Assert.IsTrue(addedRaised, "NotificationAdded イベントが発生すべき");
        Assert.IsTrue(countChangedRaised, "UnreadCountChanged イベントが発生すべき");
    }

    /// <summary>
    /// テスト観点: 重複により通知がスキップされた場合、イベントが発生しないことを確認する。
    /// </summary>
    [TestMethod]
    public void Notify_ShouldNotRaiseEvents_WhenDuplicateSkipped()
    {
        // Arrange
        var n1 = new Notification("T", "M", "task1");
        _service.Notify(n1);

        var addedRaised = false;
        _service.NotificationAdded += (s, e) => addedRaised = true;

        // Act
        _service.Notify(new Notification("T", "M2", "task1")); // 重複

        // Assert
        Assert.IsFalse(addedRaised, "重複スキップ時は NotificationAdded イベントが発生すべきではない");
    }

    /// <summary>
    /// テスト観点: 指定したIDの通知を既読にできること、およびイベントが発生することを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAsRead_ShouldUpdateStateAndRaiseEvent()
    {
        // Arrange
        var n = new Notification("T", "M");
        _service.Notify(n);
        var countChangedRaised = false;
        _service.UnreadCountChanged += (s, e) => countChangedRaised = true;

        // Act
        _service.MarkAsRead(n.Id);

        // Assert
        Assert.AreEqual(0, _service.UnreadNotifications.Count);
        Assert.IsTrue(n.IsRead);
        Assert.IsTrue(countChangedRaised, "既読切り替え時に UnreadCountChanged が発生すべき");
    }

    /// <summary>
    /// テスト観点: すべての通知を一括で既読にできることを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAllAsRead_ShouldUpdateAllUnread()
    {
        // Arrange
        _service.Notify(new Notification("T1", "M1"));
        _service.Notify(new Notification("T2", "M2"));
        Assert.AreEqual(2, _service.UnreadNotifications.Count);

        // Act
        _service.MarkAllAsRead();

        // Assert
        Assert.AreEqual(0, _service.UnreadNotifications.Count);
    }

    /// <summary>
    /// テスト観点: 存在しないIDに対して MarkAsRead を呼んでもエラーにならないことを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAsRead_WithInvalidId_ShouldNotThrow()
    {
        // Act & Assert (Should not throw)
        _service.MarkAsRead(Guid.NewGuid());
    }
}
