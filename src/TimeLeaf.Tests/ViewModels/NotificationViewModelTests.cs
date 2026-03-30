using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class NotificationViewModelTests
{
    private Mock<INotificationService> _notificationServiceMock = default!;

    [TestInitialize]
    public void TestInitialize()
    {
        _notificationServiceMock = new Mock<INotificationService>();
    }

    /// <summary>
    /// テスト観点: Notification エンティティのプロパティが ViewModel に正しく反映される。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetPropertiesFromEntity()
    {
        // Arrange
        var entity = new Notification("Title", "Message", "RelatedId");

        // Act
        var viewModel = new NotificationViewModel(entity, _notificationServiceMock.Object, _ => { });

        // Assert
        Assert.AreEqual(entity.Id, viewModel.Id);
        Assert.AreEqual(entity.Title, viewModel.Title);
        Assert.AreEqual(entity.Message, viewModel.Message);
        Assert.AreEqual(entity.CreatedAt, viewModel.CreatedAt);
    }

    /// <summary>
    /// テスト観点: MarkAsReadCommand を実行すると、通知サービス経由で既読にマークされる。
    /// </summary>
    [TestMethod]
    public void MarkAsReadCommand_ShouldCallService()
    {
        // Arrange
        var entity = new Notification("Title", "Message");
        var viewModel = new NotificationViewModel(entity, _notificationServiceMock.Object, _ => { });

        // Act
        viewModel.MarkAsReadCommand.Execute(null);

        // Assert
        _notificationServiceMock.Verify(x => x.MarkAsRead(entity.Id), Times.Once);
    }

    /// <summary>
    /// テスト観点: NavigateCommand を実行すると、提供されたナビゲーション用アクションが呼び出される。
    /// </summary>
    [TestMethod]
    public void NavigateCommand_ShouldInvokeAction()
    {
        // Arrange
        var entity = new Notification("Title", "Message", "RelatedId");
        bool actionCalled = false;
        var viewModel = new NotificationViewModel(entity, _notificationServiceMock.Object, vm =>
        {
            actionCalled = true;
            Assert.AreEqual(entity.Id, vm.Id);
        });

        // Act
        viewModel.NavigateCommand.Execute(null);

        // Assert
        Assert.IsTrue(actionCalled);
    }
}
