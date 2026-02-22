using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class EntityEnumTests
{
    /// <summary>
    /// テスト観点: Project エンティティに状態(Status)と健全性(Health)が追加され、
    /// デフォルト値が適切であることを確認する。
    /// </summary>
    [TestMethod]
    public void Project_ShouldHaveStatusAndHealth()
    {
        // Arrange & Act
        var project = new Project();

        // Assert
        Assert.AreEqual(ProjectStatus.Initial, project.Status);
        Assert.AreEqual(ProjectHealth.Healthy, project.HealthStatus);
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
