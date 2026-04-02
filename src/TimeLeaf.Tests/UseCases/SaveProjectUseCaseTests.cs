using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SaveProjectUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectServiceMock = new Mock<IProjectService>();
    }

    /// <summary>
    /// テスト観点: 単一プロジェクト保存ユースケースがサービスの SaveProjectAsync を正しく呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldCallSaveOnService()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Single Project");
        var useCase = new SaveProjectUseCase(_projectServiceMock.Object);

        // Act
        await useCase.ExecuteAsync(project);

        // Assert
        _projectServiceMock.Verify(s => s.SaveProjectAsync(project), Times.Once);
    }

    /// <summary>
    /// テスト観点: 複数プロジェクト保存ユースケースがサービスの SaveAllAsync を正しく呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_WithMultipleProjects_ShouldCallSaveAllOnService()
    {
        // Arrange
        var project1 = new Project();
        project1.UpdateName("P1");
        var project2 = new Project();
        project2.UpdateName("P2");
        var projects = new[] { project1, project2 };

        var useCase = new SaveProjectUseCase(_projectServiceMock.Object);

        // Act
        await ((ISaveProjectUseCase)useCase).ExecuteAsync(projects);

        // Assert
        _projectServiceMock.Verify(s => s.SaveAllAsync(projects), Times.Once);
    }
}
