using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;
using TimeLeaf.Utilities;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class GetProjectUpcomingDeadlinesUseCaseTests
{
    private Mock<IDateTimeProvider> _dateTimeProviderMock = null!;
    private readonly DateTime _fixedToday = new DateTime(2026, 5, 4);

    [TestInitialize]
    public void Setup()
    {
        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.Setup(x => x.Now).Returns(_fixedToday);
    }

    /// <summary>
    /// テスト観点: プロジェクト内の「3日以内」に期限が来る未完了タスクが抽出されることを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_ShouldReturnTasksDueWithinThreeDays()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());

        // 期限: 明日 (対象)
        var task1 = new ProjectTask();
        task1.UpdateName("Due Tomorrow");
        task1.UpdateSchedule(null, _fixedToday.AddDays(1));

        // 期限: 3日後 (対象)
        var task2 = new ProjectTask();
        task2.UpdateName("Due In 3 Days");
        task2.UpdateSchedule(null, _fixedToday.AddDays(3));

        // 期限: 4日後 (対象外)
        var task3 = new ProjectTask();
        task3.UpdateName("Due In 4 Days");
        task3.UpdateSchedule(null, _fixedToday.AddDays(4));

        // 期限: 過去 (対象外・期限切れとして扱う)
        var task4 = new ProjectTask();
        task4.UpdateName("Overdue");
        task4.UpdateSchedule(null, _fixedToday.AddDays(-1));

        // 完了済み (対象外)
        var task5 = new ProjectTask();
        task5.UpdateName("Done");
        task5.UpdateSchedule(null, _fixedToday.AddDays(1));
        task5.UpdateStatus(TaskStatus.Completed);

        project.AddTask(task1);
        project.AddTask(task2);
        project.AddTask(task3);
        project.AddTask(task4);
        project.AddTask(task5);

        var useCase = new GetProjectUpcomingDeadlinesUseCase(_dateTimeProviderMock.Object);

        // Act
        var result = useCase.Execute(project, 3);

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.Any(t => t.Name == "Due Tomorrow"));
        Assert.IsTrue(result.Any(t => t.Name == "Due In 3 Days"));
    }
}
