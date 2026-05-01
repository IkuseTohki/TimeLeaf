using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class EntityEnumTests
{
    [TestMethod]
    public void Project_ShouldHaveStatus()
    {
        // Arrange & Act
        var project = new Project(Guid.Empty);

        // Assert
        Assert.AreEqual(ProjectStatus.Initial, project.Status);
    }

    /// <summary>
    /// テスト観点: ProjectTask エンティティに状態(Status)と優先度(Priority)が追加され、
    /// デフォルト値が適切であることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectTask_ShouldHaveStatusAndPriority()
    {
        // Arrange & Act
        var task = new ProjectTask();

        // Assert
        Assert.AreEqual(TaskStatus.NotStarted, task.Status);
        Assert.AreEqual(TaskPriority.Medium, task.Priority);
    }
}
