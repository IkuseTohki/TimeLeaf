using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

using System;
using System.Linq;

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
        project.UpdateName("Initial Name");

        // Act
        project.UpdateName("Updated Name");

        // Assert
        Assert.AreEqual("Updated Name", project.Name);
    }

    /// <summary>
    /// テスト観点: プロジェクト全体の合計見積工数と合計実績工数が正しく算出されることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldReflectTaskCosts()
    {
        // Arrange
        var project = new Project();
        var t1 = new ProjectTask();
        t1.UpdateName("T1");
        t1.UpdateEstimatedCost(10);
        t1.UpdateActualCost(5);
        project.AddTask(t1);

        var t2 = new ProjectTask();
        t2.UpdateName("T2");
        t2.UpdateEstimatedCost(20);
        t2.UpdateActualCost(15);
        project.AddTask(t2);

        // Act & Assert
        Assert.AreEqual(30.0, project.TotalEstimatedCost, "合計見積工数が正しく算出されること");
        Assert.AreEqual(20.0, project.TotalActualCost, "合計実績工数が正しく算出されること");
    }

    /// <summary>
    /// テスト観点: タスクがない場合、合計工数は 0 となることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldBeZero_WhenNoTasks()
    {
        // Arrange
        var project = new Project();

        // Act & Assert
        Assert.AreEqual(0.0, project.TotalEstimatedCost);
        Assert.AreEqual(0.0, project.TotalActualCost);
    }

    /// <summary>
    /// テスト観点: Project エンティティにマイルストーンを追加し、正しく保持できることを確認する。
    /// </summary>
    [TestMethod]
    public void Milestones_ShouldBeReadAndWrite()
    {
        // Arrange
        var project = new Project();
        var milestoneDate = new DateTime(2026, 12, 31);
        var milestoneLabel = "Release v1.0";

        // Act
        project.AddMilestone(new Milestone(milestoneDate, milestoneLabel));

        // Assert
        Assert.IsNotNull(project.Milestones);
        Assert.AreEqual(1, project.Milestones.Count, "マイルストーンが1つ追加されていること");
        Assert.AreEqual(milestoneDate, project.Milestones.First().Date, "日付が正しく保持されていること");
        Assert.AreEqual(milestoneLabel, project.Milestones.First().Label, "ラベルが正しく保持されていること");
    }
}
