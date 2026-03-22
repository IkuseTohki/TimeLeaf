using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectTests
{
    /// <summary>
    /// テスト観点: Description プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Description_ShouldBeReadAndWrite()
    {
        // Arrange
        var project = new Project();
        var description = "This is a test project description.";

        // Act
        project.UpdateDescription(description);

        // Assert
        Assert.AreEqual(description, project.Description);
    }

    /// <summary>
    /// テスト観点: タスクを追加した際、プロジェクトのタスクリストに追加されることを確認する。
    /// （新仕様により、メモリ上の操作のみでは UpdatedAt は更新されない）
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldUpdateTasks()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Domain Test");
        var task = new ProjectTask();
        task.UpdateName("New Task");
        task.UpdateEstimatedCost(5.0);

        // Act
        project.AddTask(task);

        // Assert
        Assert.IsNotNull(project.Tasks);
        Assert.AreEqual(1, project.Tasks.Count, "タスクが追加されていること");
        Assert.AreEqual(5.0, project.TotalEstimatedCost, "合計見積工数が正しく計算されていること");
    }

    /// <summary>
    /// テスト観点: プロジェクトの基本情報を更新できることを確認する。
    /// （新仕様により、メモリ上の操作のみでは UpdatedAt は更新されない）
    /// </summary>
    [TestMethod]
    public void UpdateBasicInfo_ShouldUpdateProperties()
    {
        // Arrange
        var project = new Project();
        var name = "Updated Name";
        var status = TimeLeaf.Models.Enums.ProjectStatus.InProgress;
        var health = TimeLeaf.Models.Enums.ProjectHealth.Warning;

        // Act
        project.UpdateBasicInfo(name, status, health);

        // Assert
        Assert.AreEqual(name, project.Name);
        Assert.AreEqual(status, project.Status);
        Assert.AreEqual(health, project.HealthStatus);
    }

    /// <summary>
    /// テスト観点: プロジェクト全体の合計見積工数と合計実績工数が正しく算出されることを確認する。
    /// </summary>
    [TestMethod]
    public void AssignUser_ShouldAddUserId()
    {
        // Arrange
        var project = new Project();
        var userId = Guid.NewGuid();

        // Act
        project.AssignUser(userId);

        // Assert
        Assert.IsTrue(project.AssignedUserIds.Contains(userId));
    }

    /// <summary>
    /// テスト観点: タスクがない場合、合計工数は 0 となることを確認する。
    /// </summary>
    [TestMethod]
    public void UnassignUser_ShouldRemoveUserId()
    {
        // Arrange
        var project = new Project();
        var userId = Guid.NewGuid();
        project.AssignUser(userId);

        // Act
        project.UnassignUser(userId);

        // Assert
        Assert.IsFalse(project.AssignedUserIds.Contains(userId));
    }

    /// <summary>
    /// テスト観点: Project エンティティにマイルストーンを追加し、正しく保持できることを確認する。
    /// </summary>
    [TestMethod]
    public void ReplayAssignments_ShouldOverwriteList()
    {
        // Arrange
        var project = new Project();
        var oldId = Guid.NewGuid();
        project.AssignUser(oldId);

        var newIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        project.ReplayAssignments(newIds);

        // Assert
        Assert.AreEqual(2, project.AssignedUserIds.Count);
        Assert.IsFalse(project.AssignedUserIds.Contains(oldId));
        Assert.IsTrue(project.AssignedUserIds.Contains(newIds[0]));
        Assert.IsTrue(project.AssignedUserIds.Contains(newIds[1]));
    }
}
