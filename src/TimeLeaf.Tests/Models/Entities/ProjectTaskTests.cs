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
        task.UpdateDescription(description);

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
        task.UpdateSchedule(null, deadline);
        task.UpdateEstimatedCost(expectedEstimatedCost);
        task.UpdateActualCost(expectedActualCost);

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
        task.AssignTo(assignee);

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
        // Dependencies は List<Guid> で init プロパティなので、中の操作は可能
        task.Dependencies.Add(dependencyId);

        // Assert
        CollectionAssert.Contains(task.Dependencies, dependencyId);
    }

    /// <summary>
    /// テスト観点: 予定開始日と期限を一括で更新できること。
    /// </summary>
    [TestMethod]
    public void UpdateSchedule_ShouldSetProperties()
    {
        // Arrange
        var task = new ProjectTask();
        var start = DateTime.Today;
        var deadline = DateTime.Today.AddDays(7);

        // Act
        task.UpdateSchedule(start, deadline);

        // Assert
        Assert.AreEqual(start, task.ScheduledStartDate);
        Assert.AreEqual(deadline, task.Deadline);
    }

    /// <summary>
    /// テスト観点: ステータスを「着手中」に変更した際、開始日が未設定なら自動で設定されること。
    /// </summary>
    [TestMethod]
    public void UpdateStatus_ToInProgress_ShouldSetActualStartDate()
    {
        // Arrange
        var task = new ProjectTask();
        task.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.NotStarted);

        // Act
        task.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress);

        // Assert
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.InProgress, task.Status);
        Assert.IsNotNull(task.ActualStartDate, "着手中になったら開始日が自動設定されるべき");
    }

    /// <summary>
    /// テスト観点: 名前を空に更新しようとした場合、例外がスローされること。
    /// </summary>
    [TestMethod]
    public void UpdateName_ShouldThrow_IfEmpty()
    {
        // Arrange
        var task = new ProjectTask();

        // Act & Assert
        try
        {
            task.UpdateName("");
            Assert.Fail("空の名前は例外をスローすべき");
        }
        catch (ArgumentException) { }
    }
}
