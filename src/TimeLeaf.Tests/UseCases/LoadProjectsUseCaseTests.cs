using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class LoadProjectsUseCaseTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
    }

    /// <summary>
    /// テスト観点: ユースケースを実行した際、リポジトリから取得したプロジェクトリストが返されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldReturnProjectsFromRepository()
    {
        // Arrange
        var p1 = new Project();
        p1.UpdateName("P1");
        var expectedProjects = new List<Project> { p1 };
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(expectedProjects);
        var useCase = new LoadProjectsUseCase(_repositoryMock.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("P1", result.First().Name);
        _repositoryMock.Verify(r => r.LoadAllAsync(), Times.Once);
    }
}
