using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

using TaskStatus = TimeLeaf.Models.Enums.TaskStatus;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class CheckTaskDeadlinesUseCaseTests
{
    private Mock<INotificationService> _notificationServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
    }

    /// <summary>
    /// テスト観点: 期限切れの未完了タスクがある場合、通知が発行されることを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithOverdueTask_ShouldSendNotification()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");

        var task = new ProjectTask();
        task.UpdateName("Overdue Task");
        // 期限を昨日に設定
        task.UpdateSchedule(null, DateTime.UtcNow.AddDays(-1));
        task.UpdateStatus(TaskStatus.NotStarted);

        project.AddTask(task);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(x => x.Notify(It.Is<Notification>(n =>
            n.Title.Contains("期限切れ") &&
            n.Message.Contains("Overdue Task") &&
            n.RelatedEntityId == task.Id.ToString())), Times.Once);
    }

    /// <summary>
    /// テスト観点: 期限内、または完了済みのタスクについては通知が発行されないことを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithHealthyTasks_ShouldNotSendNotification()
    {
        // Arrange
        var project = new Project();
        var task1 = new ProjectTask();
        task1.UpdateName("Future Task");
        task1.UpdateSchedule(null, DateTime.UtcNow.AddDays(1));

        var task2 = new ProjectTask();
        task2.UpdateName("Done Task");
        task2.UpdateSchedule(null, DateTime.UtcNow.AddDays(-1));
        task2.UpdateStatus(TaskStatus.Completed);

        project.AddTask(task1);
        project.AddTask(task2);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(x => x.Notify(It.IsAny<Notification>()), Times.Never);
    }
}
