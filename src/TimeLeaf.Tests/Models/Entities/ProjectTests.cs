using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

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
        project.Description = description;

        // Assert
        Assert.AreEqual(description, project.Description);
    }

    /// <summary>
    /// テスト観点: プロジェクト全体の合計見積工数と合計実績工数が正しく算出されることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldReflectTaskCosts()
    {
        // Arrange
        var project = new Project();
        project.Tasks.Add(new ProjectTask { Name = "T1", EstimatedCost = 10, ActualCost = 5 });
        project.Tasks.Add(new ProjectTask { Name = "T2", EstimatedCost = 20, ActualCost = 15 });

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
}
