using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class MoveTaskUseCaseTests
{
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
    }

    /// <summary>
    /// テスト観点: MoveTaskUseCase が、タスクの ParentId を正しく更新し、プロジェクトを保存することを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldUpdateParentIdAndSaveProject()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var container = new ProjectContainer(Guid.NewGuid(), "Container");
        var task = new ProjectTask();

        project.AddContainer(container);
        project.AddTask(task);

        var useCase = new MoveTaskUseCase(_saveUseCaseMock.Object);

        // Act
        await useCase.ExecuteAsync(project, task.Id, container.Id);

        // Assert
        Assert.AreEqual(container.Id, task.ParentId);
        _saveUseCaseMock.Verify(s => s.ExecuteAsync(project), Times.Once);
    }

    /// <summary>
    /// テスト観点: newParentId に null を指定した場合、ParentId が null に更新されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithNullParentId_ShouldClearParentId()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var container = new ProjectContainer(Guid.NewGuid(), "Container");
        var task = new ProjectTask();
        task.SetParentId(container.Id);

        project.AddContainer(container);
        project.AddTask(task);

        var useCase = new MoveTaskUseCase(_saveUseCaseMock.Object);

        // Act
        await useCase.ExecuteAsync(project, task.Id, null);

        // Assert
        Assert.IsNull(task.ParentId);
        _saveUseCaseMock.Verify(s => s.ExecuteAsync(project), Times.Once);
    }

    /// <summary>
    /// テスト観点: 存在しないタスクを指定した場合、例外がスローされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentTask_ShouldThrowException()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var useCase = new MoveTaskUseCase(_saveUseCaseMock.Object);

        // Act & Assert
        try
        {
            await useCase.ExecuteAsync(project, Guid.NewGuid(), null);
            Assert.Fail("Expected exception was not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }

    /// <summary>
    /// テスト観点: 存在しないコンテナを移動先に指定した場合、例外がスローされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithNonExistentContainer_ShouldThrowException()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task = new ProjectTask();
        project.AddTask(task);

        var useCase = new MoveTaskUseCase(_saveUseCaseMock.Object);

        // Act & Assert
        try
        {
            await useCase.ExecuteAsync(project, task.Id, Guid.NewGuid());
            Assert.Fail("Expected exception was not thrown.");
        }
        catch (InvalidOperationException)
        {
            // Success
        }
    }
}
