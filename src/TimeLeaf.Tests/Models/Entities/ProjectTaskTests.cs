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
}
