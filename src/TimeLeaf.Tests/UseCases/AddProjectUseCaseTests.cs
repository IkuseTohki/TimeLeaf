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
    private Mock<IIdentityService> _identityServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(s => s.CurrentUserId).Returns(Guid.NewGuid());
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldCreateAndSaveProject()
    {
        // Arrange
        var useCase = new AddProjectUseCase(_projectServiceMock.Object, _identityServiceMock.Object);
        var name = "New Project";
        var description = "Description";
        var status = TimeLeaf.Models.Enums.ProjectStatus.InProgress;

        // Act
        var project = await useCase.ExecuteAsync(name, description, status);

        // Assert
        Assert.IsNotNull(project);
        Assert.AreEqual(name, project.Name);
        Assert.AreEqual(description, project.Description);
        Assert.AreEqual(status, project.Status);

        // サービスの保存が呼び出されていること
        _projectServiceMock.Verify(s => s.SaveProjectAsync(It.Is<Project>(p => p.Id == project.Id)), Times.Once);
    }

    /// <summary>
    /// テスト観点: AddProjectUseCase でプロジェクトを作成した際、作成者が AssignedUserIds に含まれていることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldAddCreatorToAssignedUserIds()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        _identityServiceMock.Setup(s => s.CurrentUserId).Returns(creatorId);
        var useCase = new AddProjectUseCase(_projectServiceMock.Object, _identityServiceMock.Object);

        // Act
        var project = await useCase.ExecuteAsync("Test", "Desc", TimeLeaf.Models.Enums.ProjectStatus.Initial);

        // Assert
        Assert.AreEqual(creatorId, project.CreatedBy);
        Assert.IsTrue(project.AssignedUserIds.Contains(creatorId), "作成者が AssignedUserIds に含まれていること");
    }
}
