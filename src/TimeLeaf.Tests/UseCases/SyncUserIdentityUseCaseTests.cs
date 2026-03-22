using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
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
        var project = new Project();

        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.GetCurrentIdentityAsync()).ReturnsAsync(myIdentity);

        var mockUserRepository = new Mock<IUserRepository>();

        var useCase = new SyncUserIdentityUseCase(
            mockIdentityService.Object,
            mockUserRepository.Object);

        // Act
        await useCase.ExecuteAsync();

        // Assert
        // プロフィールが保存されたか
        mockUserRepository.Verify(r => r.SaveUserAsync(myIdentity), Times.Once);
    }
}
