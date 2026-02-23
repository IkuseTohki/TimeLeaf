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
    /// テスト観点: タスクを追加した際、プロジェクトのタスクリストに追加され、
    /// かつ最終更新日時 (UpdatedAt) が更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldUpdateTasksAndUpdatedAt()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Domain Test");
        var initialUpdateAt = project.UpdatedAt;
        var task = new ProjectTask { Name = "New Task", EstimatedCost = 5.0 };

        // 実行時間を稼ぐために少し待機
        System.Threading.Thread.Sleep(10);

        // Act
        project.AddTask(task);

        // Assert
        Assert.AreEqual(1, project.Tasks.Count, "タスクが追加されていること");
        Assert.IsTrue(project.UpdatedAt > initialUpdateAt, "タスク追加により最終更新日時が更新されていること");
        Assert.AreEqual(5.0, project.TotalEstimatedCost, "合計見積工数が正しく計算されていること");
    }

    /// <summary>
    /// テスト観点: プロジェクトの基本情報を更新した際、
    /// 最終更新日時 (UpdatedAt) が更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void UpdateBasicInfo_ShouldRefreshUpdatedAt()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Initial Name");
        var initialUpdateAt = project.UpdatedAt;

        System.Threading.Thread.Sleep(10);

        // Act
        project.UpdateName("Updated Name");

        // Assert
        Assert.AreEqual("Updated Name", project.Name);
        Assert.IsTrue(project.UpdatedAt > initialUpdateAt, "名前更新により最終更新日時が更新されていること");
    }

    /// <summary>
    /// テスト観点: プロジェクト全体の合計見積工数と合計実績工数が正しく算出されることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldReflectTaskCosts()
    {
        // Arrange
        var project = new Project();
        project.AddTask(new ProjectTask { Name = "T1", EstimatedCost = 10, ActualCost = 5 });
        project.AddTask(new ProjectTask { Name = "T2", EstimatedCost = 20, ActualCost = 15 });

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
        project.AddMilestone(new Milestone { Date = milestoneDate, Label = milestoneLabel });

        // Assert
        Assert.AreEqual(1, project.Milestones.Count, "マイルストーンが1つ追加されていること");
        Assert.AreEqual(milestoneDate, project.Milestones.First().Date, "日付が正しく保持されていること");
        Assert.AreEqual(milestoneLabel, project.Milestones.First().Label, "ラベルが正しく保持されていること");
    }
}
