using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services.Tests;

[TestClass]
public class UserServiceTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private UserService _userService = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _userService = new UserService(_userRepositoryMock.Object);
    }

    [TestMethod]
    public async Task GetUserAsync_WhenGuidIsEmpty_ReturnsNull()
    {
        // Arrange
        /* テスト観点: Guid.Emptyを指定した場合、リポジトリを呼び出さずにnullを返すことを確認する。 */

        // Act
        var result = await _userService.GetUserAsync(Guid.Empty);

        // Assert
        Assert.IsNull(result);
        _userRepositoryMock.Verify(r => r.GetUserAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task GetUserAsync_WhenNotInCache_FetchesFromRepositoryAndCaches()
    {
        // Arrange
        /* テスト観点: キャッシュに存在しない場合、リポジトリから取得し、以降はキャッシュから返すことを確認する。 */
        var userId = Guid.NewGuid();
        var expectedUser = new User(userId, "Test User", "#FFFFFF", "");
        _userRepositoryMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync(expectedUser);

        // Act 1 (Fetch from repository)
        var result1 = await _userService.GetUserAsync(userId);

        // Assert 1
        Assert.AreEqual(expectedUser, result1);
        _userRepositoryMock.Verify(r => r.GetUserAsync(userId), Times.Once);

        // Act 2 (Fetch from cache)
        var result2 = await _userService.GetUserAsync(userId);

        // Assert 2
        Assert.AreEqual(expectedUser, result2);
        _userRepositoryMock.Verify(r => r.GetUserAsync(userId), Times.Once); // 2回目は呼ばれない
    }

    [TestMethod]
    public async Task RepositoryUserChanged_ShouldTriggerReloadAndServiceEvent()
    {
        // Arrange
        /* テスト観点: IUserRepository で UserChanged が発生した際、UserService が最新情報を再ロードし、
           自身も UserChanged イベントを発行することを確認する。 */
        var userId = Guid.NewGuid();
        var updatedUser = new User(userId, "Updated User", "#00FF00", "new_icon.png");

        _userRepositoryMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync(updatedUser);

        User? raisedUser = null;
        _userService.UserChanged += (u) => raisedUser = u;

        // Act - リポジトリのイベントをシミュレート
        _userRepositoryMock.Raise(r => r.UserChanged += null!, null!, userId);

        // 非同期の再ロードを待機
        await Task.Delay(100);

        // Assert
        Assert.IsNotNull(raisedUser, "UserService のイベントが発行されること");
        Assert.AreEqual("Updated User", raisedUser.DisplayName);

        // キャッシュも更新されているはず
        var cached = await _userService.GetUserAsync(userId);
        Assert.AreEqual("Updated User", cached?.DisplayName);
    }

    /// <summary>
    /// テスト観点: GetUserName がキャッシュヒット時に正しい表示名を返すことを確認する。
    /// </summary>
    [TestMethod]
    public void GetUserName_WhenCached_ReturnsDisplayName()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "Cache Hit User", "#000000", "");
        _userService.UpdateCache(user);

        // Act
        var result = _userService.GetUserName(userId.ToString());

        // Assert
        Assert.AreEqual("Cache Hit User", result);
    }

    /// <summary>
    /// テスト観点: GetUserName がキャッシュにない場合、"Unknown" を返すことを確認する。
    /// </summary>
    [TestMethod]
    public void GetUserName_WhenNotCached_ReturnsUnknown()
    {
        // Act
        var result = _userService.GetUserName(Guid.NewGuid().ToString());

        // Assert
        Assert.AreEqual("Unknown", result);
    }

    /// <summary>
    /// テスト観点: GetUserName に空文字や null が渡された場合、"Unassigned" を返すことを確認する。
    /// </summary>
    [TestMethod]
    public void GetUserName_WhenEmptyOrNull_ReturnsUnassigned()
    {
        // Act & Assert
        Assert.AreEqual("Unassigned", _userService.GetUserName(""));
        Assert.AreEqual("Unassigned", _userService.GetUserName(null!));
    }

    /// <summary>
    /// テスト観点: GetUserName に GUID ではない文字列が渡された場合、そのまま返すことを確認する。
    /// </summary>
    [TestMethod]
    public void GetUserName_WhenInvalidFormat_ReturnsInputString()
    {
        // Arrange
        var invalidId = "not-a-guid-but-maybe-a-system-account";

        // Act
        var result = _userService.GetUserName(invalidId);

        // Assert
        Assert.AreEqual(invalidId, result);
    }

    /// <summary>
    /// テスト観点: GetActiveUsersAsync が削除されていないユーザーのみを返すことを確認する。
    /// </summary>
    [TestMethod]
    public async Task GetActiveUsersAsync_ShouldReturnOnlyNonDeletedUsers()
    {
        // Arrange
        var user1 = new User(Guid.NewGuid(), "Active User", "#000000", "", false);
        var user2 = new User(Guid.NewGuid(), "Deleted User", "#000000", "", true);

        _userRepositoryMock.Setup(r => r.GetAllUsersAsync()).ReturnsAsync(new[] { user1, user2 });

        // Act
        var result = await _userService.GetActiveUsersAsync();

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.IsTrue(result.Any(u => u.Id == user1.Id));
        Assert.IsFalse(result.Any(u => u.Id == user2.Id));
    }

    /// <summary>
    /// テスト観点: DeleteUserAsync がユーザーを論理削除し、リポジトリに保存することを確認する。
    /// </summary>
    [TestMethod]
    public async Task DeleteUserAsync_ShouldMarkAsDeletedAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User(userId, "To Be Deleted", "#000000", "", false);

        _userRepositoryMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync(user);

        // Act
        await _userService.DeleteUserAsync(userId);

        // Assert
        Assert.IsTrue(user.IsDeleted, "ユーザーが論理削除状態になっていること");
        _userRepositoryMock.Verify(
            r => r.SaveUserAsync(It.Is<User>(u => u.Id == userId && u.IsDeleted)),
            Times.Once,
            "削除済みプロフィールが保存されること"
        );
    }
}
