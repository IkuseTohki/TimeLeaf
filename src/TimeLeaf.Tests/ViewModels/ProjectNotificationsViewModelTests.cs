using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectNotificationsViewModelTests
{
    private Mock<INotificationService> _notificationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
    }

    /// <summary>
    /// テスト観点: 初期化時に未読通知が正しくロードされることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldLoadUnreadNotifications()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new Notification("Title 1", "Message 1"),
            new Notification("Title 2", "Message 2")
        };
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(notifications);

        // Act
        var viewModel = new ProjectNotificationsViewModel(_notificationServiceMock.Object);

        // Assert
        Assert.AreEqual(2, viewModel.UnreadNotifications.Count);
        Assert.AreEqual("Title 1", viewModel.UnreadNotifications[0].Title);
    }

    /// <summary>
    /// テスト観点: 通知サービス側でカウントが変更された際、ViewModel のリストが更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void RefreshNotifications_ShouldBeCalled_OnUnreadCountChanged()
    {
        // Arrange
        var notifications = new List<Notification> { new Notification("T1", "M1") };
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(notifications);
        var viewModel = new ProjectNotificationsViewModel(_notificationServiceMock.Object);

        // 通知リストを更新
        var newNotifications = new List<Notification>
        {
            new Notification("T1", "M1"),
            new Notification("T2", "M2")
        };
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(newNotifications);

        // Act
        _notificationServiceMock.Raise(x => x.UnreadCountChanged += null, EventArgs.Empty);

        // Assert
        Assert.AreEqual(2, viewModel.UnreadNotifications.Count);
    }

    /// <summary>
    /// テスト観点: MarkAsReadCommand が通知サービスを呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAsReadCommand_ShouldCallService()
    {
        // Arrange
        var notification = new Notification("T1", "M1");
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification> { notification });
        var viewModel = new ProjectNotificationsViewModel(_notificationServiceMock.Object);

        // Act
        viewModel.MarkAsReadCommand.Execute(notification);

        // Assert
        _notificationServiceMock.Verify(x => x.MarkAsRead(notification.Id), Times.Once);
    }

    /// <summary>
    /// テスト観点: MarkAllAsReadCommand が通知サービスを呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAllAsReadCommand_ShouldCallService()
    {
        // Arrange
        var viewModel = new ProjectNotificationsViewModel(_notificationServiceMock.Object);

        // Act
        viewModel.MarkAllAsReadCommand.Execute(null);

        // Assert
        _notificationServiceMock.Verify(x => x.MarkAllAsRead(), Times.Once);
    }
}
