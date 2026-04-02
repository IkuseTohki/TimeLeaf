using System;
using System.Threading.Tasks;
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
    public void UpdateCache_CachesUserAndFiresEvent()
    {
        // Arrange
        /* テスト観点: UpdateCacheを呼び出した際、キャッシュが更新され、UserChangedイベントが発火されることを確認する。 */
        var userId = Guid.NewGuid();
        var user = new User(userId, "Updated User", "#FFFFFF", "");
        User? eventArgs = null;
        _userService.UserChanged += (u) => eventArgs = u;

        // Act
        _userService.UpdateCache(user);

        // Assert
        Assert.AreEqual(user, eventArgs);

        // キャッシュされているか確認
        var cachedUser = _userService.GetUserAsync(userId).Result;
        Assert.AreEqual(user, cachedUser);
        _userRepositoryMock.Verify(r => r.GetUserAsync(It.IsAny<Guid>()), Times.Never);
    }
}
