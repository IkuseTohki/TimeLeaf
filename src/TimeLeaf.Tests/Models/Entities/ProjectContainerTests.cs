using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectContainerTests
{
    /// <summary>
    /// テスト観点: コンテナが基本的なプロパティを保持できることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetBasicProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Main Feature";
        var description = "Description of main feature";

        // Act
        var containerWithId = new ProjectContainer(id, name);
        containerWithId.UpdateDescription(description);

        // Assert
        Assert.AreEqual(id, containerWithId.Id);
        Assert.AreEqual(name, containerWithId.Name);
        Assert.AreEqual(description, containerWithId.Description);
    }

    /// <summary>
    /// テスト観点: 子要素が空の場合、集計値がデフォルト値を返すことを確認する。
    /// </summary>
    [TestMethod]
    public void Aggregation_WithEmptyChildren_ShouldReturnDefaultValues()
    {
        // Arrange
        var container = new ProjectContainer();

        // Assert
        Assert.AreEqual(0, container.ProgressPercentage);
        Assert.AreEqual(0, container.ActualHours);
        Assert.IsNull(container.ActualStartDate);
        Assert.IsNull(container.ActualEndDate);
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.NotStarted, container.Status);
    }

    /// <summary>
    /// テスト観点: 子要素に様々な状態が混在する場合、ステータスが適切に決定されることを確認する。
    /// </summary>
    [TestMethod]
    [DataRow(
        TimeLeaf.Models.Enums.TaskStatus.NotStarted,
        TimeLeaf.Models.Enums.TaskStatus.NotStarted,
        TimeLeaf.Models.Enums.TaskStatus.NotStarted
    )]
    [DataRow(
        TimeLeaf.Models.Enums.TaskStatus.Completed,
        TimeLeaf.Models.Enums.TaskStatus.Completed,
        TimeLeaf.Models.Enums.TaskStatus.Completed
    )]
    [DataRow(
        TimeLeaf.Models.Enums.TaskStatus.NotStarted,
        TimeLeaf.Models.Enums.TaskStatus.Completed,
        TimeLeaf.Models.Enums.TaskStatus.InProgress
    )]
    [DataRow(
        TimeLeaf.Models.Enums.TaskStatus.InProgress,
        TimeLeaf.Models.Enums.TaskStatus.NotStarted,
        TimeLeaf.Models.Enums.TaskStatus.InProgress
    )]
    public void Aggregation_MixedStatuses_ShouldDetermineCorrectStatus(
        TimeLeaf.Models.Enums.TaskStatus s1,
        TimeLeaf.Models.Enums.TaskStatus s2,
        TimeLeaf.Models.Enums.TaskStatus expected
    )
    {
        // Arrange
        var container = new ProjectContainer();
        var t1 = new ProjectTask();
        t1.UpdateStatus(s1);
        var t2 = new ProjectTask();
        t2.UpdateStatus(s2);

        // Act
        container.AddChild(t1);
        container.AddChild(t2);

        // Assert
        Assert.AreEqual(expected, container.Status);
    }

    /// <summary>
    /// テスト観点: コンテナが入れ子になっている場合、再帰的に集計されることを確認する。
    /// </summary>
    [TestMethod]
    public void Aggregation_Recursive_ShouldSumValues()
    {
        // Arrange
        var root = new ProjectContainer(Guid.NewGuid(), "Root");
        var sub = new ProjectContainer(Guid.NewGuid(), "Sub");

        var task = new ProjectTask();
        task.UpdateActualCost(10);
        task.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.Completed); // Progress 100

        // Act
        sub.AddChild(task);
        root.AddChild(sub);

        // Assert
        Assert.AreEqual(10, root.ActualHours);
        Assert.AreEqual(100, root.ProgressPercentage);
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.Completed, root.Status);
    }
}
