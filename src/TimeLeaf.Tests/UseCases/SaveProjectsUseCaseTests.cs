using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SaveProjectsUseCaseTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
    }

    /// <summary>
    /// テスト観点: ユースケースを実行した際、リポジトリの保存メソッドが正しく呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldCallSaveOnRepository()
    {
        // Arrange
        var projects = new List<Project> { new Project { Name = "P1" } };
        var useCase = new SaveProjectsUseCase(_repositoryMock.Object);

        // Act
        await useCase.ExecuteAsync(projects);

        // Assert
        _repositoryMock.Verify(r => r.SaveAllAsync(projects), Times.Once);
    }
}
