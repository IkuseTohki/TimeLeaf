using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SaveProjectsUseCaseTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
    }

    /// <summary>
    /// テスト観点: ユースケースを実行した際、リポジトリの保存メソッドが正しく呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldCallSaveOnRepository()
    {
        // Arrange
        var p1 = new Project();
        p1.UpdateName("P1");
        var projects = new List<Project> { p1 };
        var useCase = new SaveProjectsUseCase(_repositoryMock.Object, _userServiceMock.Object);

        // Act
        await useCase.ExecuteAsync(projects);

        // Assert
        _repositoryMock.Verify(r => r.SaveAllAsync(projects, "test-user"), Times.Once);
    }
}
