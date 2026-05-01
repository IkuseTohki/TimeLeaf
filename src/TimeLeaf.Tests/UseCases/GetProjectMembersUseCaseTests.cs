using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class GetProjectMembersUseCaseTests
{
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private GetProjectMembersUseCase _useCase = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _useCase = new GetProjectMembersUseCase(_userRepositoryMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldReturnUsers_WhenUserIdsExistInProject()
    {
        // 1. Arrange - テスト観点: プロジェクトのアサイン済みユーザーIDに対応するユーザーをリポジトリから取得できること。
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User(userId1, "User 1", "#FFFFFF", "icon1.png");
        var user2 = new User(userId2, "User 2", "#000000", "icon2.png");

        var project = new Project(Guid.Empty);
        project.UpdateName("Project 1");
        project.AssignUser(userId1);
        project.AssignUser(userId2);

        _userRepositoryMock.Setup(x => x.GetUserAsync(userId1)).ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetUserAsync(userId2)).ReturnsAsync(user2);

        // 2. Act
        var result = await _useCase.ExecuteAsync(project);

        // 3. Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.Any(u => u.Id == userId1));
        Assert.IsTrue(result.Any(u => u.Id == userId2));
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldSkipNullUsers_WhenSomeUserIdsNotFound()
    {
        // 1. Arrange - テスト観点: 一部のユーザーが見つからない場合、nullをスキップして存在するユーザーのみ返すこと。
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var user1 = new User(userId1, "User 1", "#FFFFFF", "icon1.png");

        var project = new Project(Guid.Empty);
        project.UpdateName("Project 1");
        project.AssignUser(userId1);
        project.AssignUser(userId2); // 見つからない想定

        _userRepositoryMock.Setup(x => x.GetUserAsync(userId1)).ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetUserAsync(userId2)).ReturnsAsync((User?)null);

        // 2. Act
        var result = await _useCase.ExecuteAsync(project);

        // 3. Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual(userId1, result.First().Id);
    }
}
