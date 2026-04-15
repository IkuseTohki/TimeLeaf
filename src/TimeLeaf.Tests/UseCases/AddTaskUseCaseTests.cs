using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class AddTaskUseCaseTests
{
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
    }

    /// <summary>
    /// テスト観点: AddTaskUseCase が、指定されたパラメータでタスクを作成し、
    /// プロジェクトに追加した上で、プロジェクトを保存することを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldAddTaskAndSaveProject()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");

        var useCase = new AddTaskUseCase(_saveUseCaseMock.Object);

        var taskName = "New Task";
        var description = "Description";
        var status = TimeLeaf.Models.Enums.TaskStatus.InProgress;
        var priority = TaskPriority.High;
        var assignee = "User A";

        // Act
        await useCase.ExecuteAsync(
            project,
            taskName,
            description,
            status,
            priority,
            null, // scheduledStart
            null, // deadline
            null, // actualStart
            null, // actualEnd
            10.5, // estimatedCost
            0.0, // actualCost
            assignee
        );

        // Assert
        // 1. プロジェクトにタスクが追加されていること
        Assert.AreEqual(1, project.Tasks.Count());
        var addedTask = project.Tasks.First();
        Assert.AreEqual(taskName, addedTask.Name);
        Assert.AreEqual(description, addedTask.Description);
        Assert.AreEqual(status, addedTask.Status);
        Assert.AreEqual(priority, addedTask.Priority);
        Assert.AreEqual(assignee, addedTask.Assignee);
        Assert.AreEqual(10.5, addedTask.EstimatedCost);

        // 2. プロジェクトの保存が呼び出されていること
        _saveUseCaseMock.Verify(s => s.ExecuteAsync(project), Times.Once);
    }
}
