using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SyncUserIdentityUseCaseTests
{
    /// <summary>
    /// テスト観点: 自分の情報がユーザーリポジトリに保存されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Execute_ShouldSyncIdentityToUserRepository()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var myIdentity = new User(myId, "自分", "#00FF00", "my.png");
        var project = new Project(Guid.Empty);

        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.GetCurrentIdentityAsync()).ReturnsAsync(myIdentity);

        var mockUserRepository = new Mock<IUserRepository>();

        var useCase = new SyncUserIdentityUseCase(mockIdentityService.Object, mockUserRepository.Object);

        // Act
        await useCase.ExecuteAsync();

        // Assert
        // プロフィールが保存されたか
        mockUserRepository.Verify(r => r.SaveUserAsync(myIdentity), Times.Once);
    }

    /// <summary>
    /// テスト観点: 既存のプロフィールが削除済みになっている場合、自動的に復帰（IsDeleted=false）されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Execute_WhenDeleted_ShouldRestore()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var myIdentity = new User(myId, "自分", "#00FF00", "my.png", false);
        var existingProfile = new User(myId, "自分(旧)", "#00FF00", "my.png", true); // 削除済み

        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.GetCurrentIdentityAsync()).ReturnsAsync(myIdentity);

        var mockUserRepository = new Mock<IUserRepository>();
        mockUserRepository.Setup(r => r.GetUserAsync(myId)).ReturnsAsync(existingProfile);

        var useCase = new SyncUserIdentityUseCase(mockIdentityService.Object, mockUserRepository.Object);

        // Act
        await useCase.ExecuteAsync();

        // Assert
        Assert.IsFalse(myIdentity.IsDeleted, "自分のアイデンティティが復帰されていること");
        mockUserRepository.Verify(
            r => r.SaveUserAsync(It.Is<User>(u => u.Id == myId && !u.IsDeleted)),
            Times.Once,
            "復帰されたプロフィールが保存されること"
        );
    }
}
