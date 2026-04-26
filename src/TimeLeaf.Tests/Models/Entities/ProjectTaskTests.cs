using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

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
        // Dependencies への直接操作ではなく AddConstraint を使用する
        task.AddConstraint(
            new TimeLeaf.Models.Entities.TaskConstraint(dependencyId, TimeLeaf.Models.Enums.TaskConstraintType.FS)
        );

        // Assert
        Assert.IsTrue(task.Constraints.Any(c => c.PredecessorId == dependencyId));
    }

    /// <summary>
    /// テスト観点: Constraints プロパティに制約を追加・取得できることを確認する。
    /// </summary>
    [TestMethod]
    public void Constraints_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var predecessorId = Guid.NewGuid();
        var constraint = new TaskConstraint(
            predecessorId,
            TimeLeaf.Models.Enums.TaskConstraintType.SS,
            2,
            "Test Constraint"
        );

        // Act
        task.AddConstraint(constraint);

        // Assert
        Assert.AreEqual(1, task.Constraints.Count);
        Assert.AreEqual(constraint, task.Constraints[0]);
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
    /// テスト観点: 子タスクを持つコンテナタスクが、子タスクの範囲に基づきスケジュールと進捗を自動算出することを確認する。
    /// </summary>
    [TestMethod]
    public void ContainerTask_ShouldAggregateChildrenData()
    {
        // Arrange
        var parent = new ProjectTask();
        parent.UpdateName("Parent Container");

        var child1 = new ProjectTask();
        child1.UpdateSchedule(new DateTime(2026, 4, 1), new DateTime(2026, 4, 5));
        child1.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.Completed); // 100%

        var child2 = new ProjectTask();
        child2.UpdateSchedule(new DateTime(2026, 4, 3), new DateTime(2026, 4, 10));
        child2.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.NotStarted); // 0%

        // Act
        parent.AddChild(child1);
        parent.AddChild(child2);

        // Assert
        Assert.AreEqual(new DateTime(2026, 4, 1), parent.ScheduledStartDate, "開始日は最小値になるべき");
        Assert.AreEqual(new DateTime(2026, 4, 10), parent.Deadline, "期限は最大値になるべき");
        // (1.0 + 0.0) / 2 = 0.5 -> InProgress
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.InProgress, parent.Status, "進捗は子タスクの平均に基づくべき");
    }

    /// <summary>
    /// テスト観点: 親->子->孫の3段階層において、孫タスクの変更がルートまで正しく集計されることを確認する。
    /// </summary>
    [TestMethod]
    public void NestedContainer_ShouldAggregateUpToGrandParent()
    {
        // Arrange
        var root = new ProjectTask();
        root.UpdateName("Root");
        var parent = new ProjectTask();
        parent.UpdateName("Parent");
        var child = new ProjectTask();
        child.UpdateName("GrandChild");

        var start = new DateTime(2026, 5, 1);
        var end = new DateTime(2026, 5, 10);
        child.UpdateSchedule(start, end);
        child.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.Completed);

        // Act
        parent.AddChild(child); // 子のデータが親に反映
        root.AddChild(parent); // 親のデータがルートに反映

        // Assert
        Assert.AreEqual(start, root.ScheduledStartDate, "ルートに孫の開始日が反映されること");
        Assert.AreEqual(end, root.Deadline, "ルートに孫の期限が反映されること");
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.Completed, root.Status, "ルートに孫の進捗が反映されること");
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
