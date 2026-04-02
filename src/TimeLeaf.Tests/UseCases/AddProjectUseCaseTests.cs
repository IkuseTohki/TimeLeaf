using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class AddProjectUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectServiceMock = new Mock<IProjectService>();
    }

    /// <summary>
    /// テスト観点: AddProjectUseCase が、指定された内容でプロジェクトを作成し、保存することを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldCreateAndSaveProject()
    {
        // Arrange
        var useCase = new AddProjectUseCase(_projectServiceMock.Object);
        var name = "New Project";
        var description = "Description";
        var status = TimeLeaf.Models.Enums.ProjectStatus.InProgress;
        var health = TimeLeaf.Models.Enums.ProjectHealth.Healthy;

        // Act
        var project = await useCase.ExecuteAsync(name, description, status, health);

        // Assert
        Assert.IsNotNull(project);
        Assert.AreEqual(name, project.Name);
        Assert.AreEqual(description, project.Description);
        Assert.AreEqual(status, project.Status);
        Assert.AreEqual(health, project.HealthStatus);

        // サービスの保存が呼び出されていること
        _projectServiceMock.Verify(s => s.SaveProjectAsync(It.Is<Project>(p => p.Id == project.Id)), Times.Once);
    }
}
