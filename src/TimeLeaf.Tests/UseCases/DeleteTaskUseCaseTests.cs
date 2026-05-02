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
public class DeleteTaskUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;
    private DeleteTaskUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _useCase = new DeleteTaskUseCase(_projectServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldRemoveTaskFromProjectAndSave()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var task = new ProjectTask();
        task.UpdateName("Task to delete");
        project.AddTask(task);
        var taskId = task.Id;

        Assert.AreEqual(1, project.Tasks.Count);

        // Act
        await _useCase.ExecuteAsync(project, taskId);

        // Assert
        Assert.AreEqual(0, project.Tasks.Count, "タスクがプロジェクトから削除されていること");
        _projectServiceMock.Verify(
            x => x.SaveProjectAsync(project),
            Times.Once,
            "サービスの保存が呼び出されていること"
        );
    }

    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentTask_ShouldStillSave()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var taskId = Guid.NewGuid();

        // Act
        await _useCase.ExecuteAsync(project, taskId);

        // Assert
        _projectServiceMock.Verify(x => x.SaveProjectAsync(project), Times.Once, "存在しないタスクでも保存処理は走る");
    }
}
