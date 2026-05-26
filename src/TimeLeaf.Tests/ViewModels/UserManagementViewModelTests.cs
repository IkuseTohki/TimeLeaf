using System;
using System.Linq;
using System.Threading.Tasks;
using LeafKit.UI.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class UserManagementViewModelTests
{
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    private UserManagementViewModel CreateViewModel()
    {
        return new UserManagementViewModel(
            _userServiceMock.Object,
            _identityServiceMock.Object,
            _dialogServiceMock.Object
        );
    }

    /// <summary>
    /// テスト観点: LoadAsync によってアクティブなユーザーがロードされ、自分が先頭に配置されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task LoadAsync_ShouldLoadActiveUsers_WithMeAtTop()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var otherId1 = Guid.NewGuid();
        var otherId2 = Guid.NewGuid();
        var me = new User(myId, "自分", "#000000", "");
        var otherA = new User(otherId1, "Alice", "#FFFFFF", "");
        var otherZ = new User(otherId2, "Zebra", "#FFFFFF", "");

        _identityServiceMock.Setup(s => s.CurrentUserId).Returns(myId);
        // 順不同で返す
        _userServiceMock.Setup(s => s.GetActiveUsersAsync()).ReturnsAsync(new[] { otherZ, me, otherA });

        var viewModel = CreateViewModel();

        // Act
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual(3, viewModel.Users.Count);

        // 1番目が自分であること
        Assert.AreEqual(myId, viewModel.Users[0].Id);
        Assert.IsTrue(viewModel.Users[0].IsMe);

        // 2番目以降が名前順（Alice -> Zebra）であること
        Assert.AreEqual("Alice", viewModel.Users[1].DisplayName);
        Assert.AreEqual("Zebra", viewModel.Users[2].DisplayName);
    }

    /// <summary>
    /// テスト観点: 自分自身は削除できない（CanDelete が false）ことを確認する。
    /// </summary>
    [TestMethod]
    public async Task DeleteCommand_ShouldBeDisabledForMe()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var me = new User(myId, "自分", "#000000", "");
        _identityServiceMock.Setup(s => s.CurrentUserId).Returns(myId);
        _userServiceMock.Setup(s => s.GetActiveUsersAsync()).ReturnsAsync(new[] { me });

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        var meVm = viewModel.Users.First();

        // Assert
        Assert.IsFalse(meVm.DeleteCommand.CanExecute(null));
    }

    /// <summary>
    /// テスト観点: 他者の削除コマンド実行時、確認ダイアログで「はい」を選択した場合に削除が実行されることを確認する。
    /// </summary>
    /// <summary>
    /// 他者の削除コマンド実行時、確認ダイアログで「はい」を選択した場合に削除が実行されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task DeleteCommand_WhenConfirmed_ShouldDeleteUser()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var other = new User(otherId, "他者", "#FFFFFF", "");
        _userServiceMock.Setup(s => s.GetActiveUsersAsync()).ReturnsAsync(new[] { other });
        _dialogServiceMock.Setup(s => s.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        var viewModel = CreateViewModel();
        await viewModel.LoadAsync();
        var otherVm = viewModel.Users.First();

        // Act
        await otherVm.DeleteCommand.ExecuteAsync(null);

        // Assert
        _userServiceMock.Verify(s => s.DeleteUserAsync(otherId), Times.Once);
        Assert.IsFalse(viewModel.Users.Contains(otherVm), "削除されたユーザーがリストから消えていること");
    }
}
