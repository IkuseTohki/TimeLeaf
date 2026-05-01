using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class CalculateCriticalPathUseCaseTests
{
    /// <summary>
    /// テスト観点: 依存関係のあるタスク群から、クリティカルパス（最長経路）に含まれるタスクを正しく特定できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldIdentifyCriticalPath()
    {
        // Arrange
        var project = new Project(Guid.Empty);

        // t1: 4/1 - 4/5 (5日間)
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        t1.UpdateName("Short Task");
        t1.UpdateSchedule(new DateTime(2026, 4, 1), new DateTime(2026, 4, 5));

        // t2: 4/1 - 4/10 (10日間) -> Critical
        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        t2.UpdateName("Long Task (Critical)");
        t2.UpdateSchedule(new DateTime(2026, 4, 1), new DateTime(2026, 4, 10));

        // t3: t1, t2 の後に開始 (4/11 - 4/15)
        var t3 = new ProjectTask { Id = Guid.NewGuid() };
        t3.UpdateName("Follow-up Task (Critical)");
        t3.AddConstraint(new TaskConstraint(t1.Id));
        t3.AddConstraint(new TaskConstraint(t2.Id));
        t3.UpdateSchedule(new DateTime(2026, 4, 11), new DateTime(2026, 4, 15));

        project.AddTask(t1);
        project.AddTask(t2);
        project.AddTask(t3);

        var useCase = new CalculateCriticalPathUseCase();

        // Act
        var result = await useCase.ExecuteAsync(project);
        var criticalPathIds = result.ToList();

        // Assert
        Assert.IsTrue(criticalPathIds.Contains(t2.Id), "t2 (最長経路の起点) はクリティカルパスに含まれるべき");
        Assert.IsTrue(criticalPathIds.Contains(t3.Id), "t3 (最長経路の後続) はクリティカルパスに含まれるべき");
        Assert.IsFalse(criticalPathIds.Contains(t1.Id), "t1 (余裕がある経路) はクリティカルパスに含まれないべき");
    }

    /// <summary>
    /// テスト観点: 制約に猶予期間（LagDays）が設定されている場合、それがクリティカルパス計算に正しく反映されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldConsiderLagDays()
    {
        // Arrange
        var project = new Project(Guid.Empty);

        // t1: 4/1 - 4/5 (5日間)
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        t1.UpdateSchedule(new DateTime(2026, 4, 1), new DateTime(2026, 4, 5));

        // t2: t1 の完了から「10日空けて」開始 -> 合計期間が長くなり、t1 もクリティカルになる
        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        t2.AddConstraint(new TaskConstraint(t1.Id, TimeLeaf.Models.Enums.TaskConstraintType.FS, LagDays: 10));
        t2.UpdateSchedule(new DateTime(2026, 4, 16), new DateTime(2026, 4, 20));

        project.AddTask(t1);
        project.AddTask(t2);

        var useCase = new CalculateCriticalPathUseCase();

        // Act
        var criticalPathIds = (await useCase.ExecuteAsync(project)).ToList();

        // Assert
        Assert.IsTrue(criticalPathIds.Contains(t1.Id), "t1 は後続の大きなラグによってクリティカルになるべき");
        Assert.IsTrue(criticalPathIds.Contains(t2.Id), "t2 はクリティカルになるべき");
    }
}
