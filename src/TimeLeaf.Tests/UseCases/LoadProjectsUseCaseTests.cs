using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class LoadProjectsUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectServiceMock = new Mock<IProjectService>();
    }

    /// <summary>
    /// テスト観点: LoadProjectsUseCase がサービスを介してプロジェクト一覧を取得することを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldCallLoadAndReturnAllProjects()
    {
        // Arrange
        var projects = new List<Project> { new Project(), new Project() };
        _projectServiceMock.Setup(s => s.AllProjects).Returns(projects);

        var useCase = new LoadProjectsUseCase(_projectServiceMock.Object);

        // Act
        var result = await useCase.ExecuteAsync();

        // Assert
        _projectServiceMock.Verify(s => s.LoadAllAsync(), Times.Once);
        Assert.AreEqual(2, result.Count());
        Assert.AreSame(projects, result);
    }
}
