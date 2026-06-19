using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.Utilities;
using TaskStatus = TimeLeaf.Models.Enums.TaskStatus;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class CheckTaskDeadlinesUseCaseTests
{
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IDateTimeProvider> _dateTimeProviderMock = null!;
    private readonly DateTime _fixedToday = new DateTime(2026, 5, 4);

    [TestInitialize]
    public void Setup()
    {
        _notificationServiceMock = new Mock<INotificationService>();
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.Now).Returns(_fixedToday);
    }

    /// <summary>
    /// テスト観点: 期限切れの未完了タスクがある場合、通知が発行されることを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithOverdueTask_ShouldSendNotification()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");

        var task = new ProjectTask();
        task.UpdateName("Overdue Task");
        // 固定日の昨日を期限に設定
        task.UpdateSchedule(null, _fixedToday.AddDays(-1));
        task.UpdateStatus(TaskStatus.NotStarted);

        project.AddTask(task);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object, _dateTimeProviderMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(
            x =>
                x.Notify(
                    It.Is<Notification>(n =>
                        n.Title.Contains("期限切れ")
                        && n.Message.Contains("Overdue Task")
                        && n.RelatedEntityId == task.Id.ToString()
                    )
                ),
            Times.Once
        );
    }

    /// <summary>
    /// テスト観点: 本日が期限のタスクは、期限切れと判定されない（通知されない）ことを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithTaskDueToday_ShouldSendApproachingNotification()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task = new ProjectTask();
        task.UpdateName("Today's Task");

        // 固定日を期限に設定
        task.UpdateSchedule(null, _fixedToday);
        task.UpdateStatus(TaskStatus.InProgress);

        project.AddTask(task);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object, _dateTimeProviderMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(
            x => x.Notify(It.Is<Notification>(n => n.Title.Contains("期限間近") && n.Message.Contains("Today's Task"))),
            Times.Once
        );
    }

    /// <summary>
    /// テスト観点: 期限内、または完了済みのタスクについては通知が発行されないことを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithHealthyTasks_ShouldNotSendNotification()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task1 = new ProjectTask();
        task1.UpdateName("Future Task");
        task1.UpdateSchedule(null, _fixedToday.AddDays(2));

        var task2 = new ProjectTask();
        task2.UpdateName("Done Task");
        task2.UpdateSchedule(null, _fixedToday.AddDays(-1));
        task2.UpdateStatus(TaskStatus.Completed);

        project.AddTask(task1);
        project.AddTask(task2);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object, _dateTimeProviderMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(x => x.Notify(It.IsAny<Notification>()), Times.Never);
    }

    /// <summary>
    /// テスト観点: 期限が本日または翌日のタスクがある場合、「期限間近」の通知が発行されることを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_WithApproachingDeadlineTask_ShouldSendNotification()
    {
        // Arrange
        var project = new Project(Guid.Empty);

        var taskToday = new ProjectTask();
        taskToday.UpdateName("Due Today Task");
        taskToday.UpdateSchedule(null, _fixedToday);
        taskToday.UpdateStatus(TaskStatus.NotStarted);

        var taskTomorrow = new ProjectTask();
        taskTomorrow.UpdateName("Due Tomorrow Task");
        taskTomorrow.UpdateSchedule(null, _fixedToday.AddDays(1));
        taskTomorrow.UpdateStatus(TaskStatus.InProgress);

        project.AddTask(taskToday);
        project.AddTask(taskTomorrow);

        var useCase = new CheckTaskDeadlinesUseCase(_notificationServiceMock.Object, _dateTimeProviderMock.Object);

        // Act
        useCase.Execute(new[] { project });

        // Assert
        _notificationServiceMock.Verify(
            x =>
                x.Notify(
                    It.Is<Notification>(n => n.Title.Contains("期限間近") && n.Message.Contains("Due Today Task"))
                ),
            Times.Once
        );

        _notificationServiceMock.Verify(
            x =>
                x.Notify(
                    It.Is<Notification>(n => n.Title.Contains("期限間近") && n.Message.Contains("Due Tomorrow Task"))
                ),
            Times.Once
        );
    }
}
