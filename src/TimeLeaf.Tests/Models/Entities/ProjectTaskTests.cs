using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectTaskTests
{
    /// <summary>
    /// テスト観点: Description プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Description_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var description = "This is a test task description.";

        // Act
        task.Description = description;

        // Assert
        Assert.AreEqual(description, task.Description);
    }

    /// <summary>
    /// テスト観点: Deadline, EstimatedCost, ActualCost プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void CostProperties_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var deadline = new DateTime(2026, 3, 31);
        var expectedEstimatedCost = 12.5;
        var expectedActualCost = 10.0;

        // Act
        task.Deadline = deadline;
        task.EstimatedCost = expectedEstimatedCost;
        task.ActualCost = expectedActualCost;

        // Assert
        Assert.AreEqual(deadline, task.Deadline);
        Assert.AreEqual(expectedEstimatedCost, task.EstimatedCost);
        Assert.AreEqual(expectedActualCost, task.ActualCost);
    }

    /// <summary>
    /// テスト観点: Assignee プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Assignee_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var assignee = "user1";

        // Act
        task.Assignee = assignee;

        // Assert
        Assert.AreEqual(assignee, task.Assignee);
    }

    /// <summary>
    /// テスト観点: Dependencies プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Dependencies_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var dependencyId = Guid.NewGuid();

        // Act
        task.Dependencies.Add(dependencyId);

        // Assert
        CollectionAssert.Contains(task.Dependencies, dependencyId);
    }

    /// <summary>
    /// テスト観点: ProjectTask に詳細スケジュール属性（予定開始、実績開始、実績終了）が追加され、
    /// 正しく値を保持できることを確認する。
    /// </summary>
    [TestMethod]
    public void ScheduleProperties_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var scheduledStart = new DateTime(2026, 3, 1);
        var actualStart = new DateTime(2026, 3, 2);
        var actualEnd = new DateTime(2026, 3, 5);

        // Act
        task.ScheduledStartDate = scheduledStart;
        task.ActualStartDate = actualStart;
        task.ActualEndDate = actualEnd;

        // Assert
        Assert.AreEqual(scheduledStart, task.ScheduledStartDate, "ScheduledStartDate が正しく保持されること");
        Assert.AreEqual(task.ActualStartDate, actualStart, "ActualStartDate が正しく保持されること");
        Assert.AreEqual(task.ActualEndDate, actualEnd, "ActualEndDate が正しく保持されること");
    }
}
