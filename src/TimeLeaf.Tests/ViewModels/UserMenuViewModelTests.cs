using System;
using System.Threading.Tasks;
using LeafKit.UI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class UserMenuViewModelTests
{
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _userServiceMock = new Mock<IUserService>();
        _dialogServiceMock = new Mock<IDialogService>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
    }

    private UserMenuViewModel CreateViewModel()
    {
        return new UserMenuViewModel(
            _identityServiceMock.Object,
            _userServiceMock.Object,
            _dialogServiceMock.Object,
            _viewModelFactoryMock.Object
        );
    }

    /// <summary>
    /// テスト観点: 日本語名などの場合でも、イニシャルが先頭1文字のみであることを確認する。
    /// </summary>
    [TestMethod]
    public async Task UserInitial_ShouldBeFirstCharacter()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "佐藤 太郎", "#000000", "");
        _identityServiceMock.Setup(x => x.CurrentUserId).Returns(userId);
        _identityServiceMock.Setup(x => x.GetCurrentIdentityAsync()).ReturnsAsync(user);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(100); // Wait for LoadIdentityAsync

        // Assert
        Assert.AreEqual("佐藤 太郎", viewModel.UserName);
        Assert.AreEqual("佐", viewModel.UserInitial);
    }

    /// <summary>
    /// テスト観点: ユーザー情報が更新された際、通知を受けてプロパティが更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void UserInitial_ShouldUpdate_OnUserChangedEvent()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _identityServiceMock.Setup(x => x.CurrentUserId).Returns(userId);
        _identityServiceMock
            .Setup(x => x.GetCurrentIdentityAsync())
            .ReturnsAsync(new User(userId, "Old Name", "#000000", ""));

        var viewModel = CreateViewModel();

        // Act
        var updatedUser = new User(userId, "田中 花子", "#FFFFFF", "");
        _userServiceMock.Raise(x => x.UserChanged += null, updatedUser);

        // Assert
        Assert.AreEqual("田中 花子", viewModel.UserName);
        Assert.AreEqual("田", viewModel.UserInitial);
    }

    /// <summary>
    /// テスト観点: 空のユーザー名の場合、イニシャルが '?' になることを確認する。
    /// </summary>
    [TestMethod]
    public async Task UserInitial_ShouldBeQuestion_WhenNameIsEmpty()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "", "#000000", "");
        _identityServiceMock.Setup(x => x.CurrentUserId).Returns(userId);
        _identityServiceMock.Setup(x => x.GetCurrentIdentityAsync()).ReturnsAsync(user);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(100);

        // Assert
        Assert.AreEqual("", viewModel.UserName);
        Assert.AreEqual("?", viewModel.UserInitial);
    }
}
