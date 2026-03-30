using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class NotificationsViewModelTests
{
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<ILogger<NotificationsViewModel>> _loggerMock = null!;
    private ObservableCollection<ProjectViewModel> _projects = null!;

    [TestInitialize]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _loggerMock = new Mock<ILogger<NotificationsViewModel>>();
        _projects = new ObservableCollection<ProjectViewModel>();
    }

    /// <summary>
    /// テスト観点: 初期化時に未読通知が正しくロードされることを確認する（フィルターなし）。
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
        var viewModel = new NotificationsViewModel(_notificationServiceMock.Object, _projects, _loggerMock.Object);

        // Assert
        Assert.AreEqual(2, viewModel.UnreadNotifications.Count);
        Assert.AreEqual("Title 1", viewModel.UnreadNotifications[0].Title);
    }

    /// <summary>
    /// テスト観点: フィルター指定時、該当するプロジェクトの通知のみがロードされる。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldLoadOnlyFilteredNotifications()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var notifications = new List<Notification>
        {
            new Notification("Title 1", "Msg 1", projectId.ToString()),
            new Notification("Title 2", "Msg 2", Guid.NewGuid().ToString())
        };
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(notifications);

        // Act
        var viewModel = new NotificationsViewModel(_notificationServiceMock.Object, _projects, _loggerMock.Object, projectId);

        // Assert
        Assert.AreEqual(1, viewModel.UnreadNotifications.Count);
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
        var viewModel = new NotificationsViewModel(_notificationServiceMock.Object, _projects, _loggerMock.Object);

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
    /// テスト観点: MarkAllAsReadCommand が通知サービスを呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAllAsReadCommand_ShouldCallService()
    {
        // Arrange
        var viewModel = new NotificationsViewModel(_notificationServiceMock.Object, _projects, _loggerMock.Object);

        // Act
        viewModel.MarkAllAsReadCommand.Execute(null);

        // Assert
        _notificationServiceMock.Verify(x => x.MarkAllAsRead(), Times.Once);
    }
}
