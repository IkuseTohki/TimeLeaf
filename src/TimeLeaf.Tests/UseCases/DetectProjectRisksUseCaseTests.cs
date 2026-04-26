using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class DetectProjectRisksUseCaseTests
{
    private DetectProjectRisksUseCase _useCase = null!;

    [TestInitialize]
    public void Setup()
    {
        _useCase = new DetectProjectRisksUseCase(new CalculateCriticalPathUseCase());
    }

    /// <summary>
    /// テスト観点: 先行タスクが完了していないのに後続タスクが開始されている場合、制約違反として検出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldDetectConstraintViolation()
    {
        // Arrange
        var project = new Project();
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        t1.UpdateName("Predecessor");
        t1.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress); // 未完了

        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        t2.UpdateName("Successor");
        t2.AddConstraint(new TaskConstraint(t1.Id, TaskConstraintType.FS));
        t2.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress); // 先行未了なのに開始している

        project.AddTask(t1);
        project.AddTask(t2);

        // Act
        var risks = await _useCase.ExecuteAsync(project);

        // Assert
        Assert.IsTrue(
            risks.Any(r => r.TaskId == t2.Id && r.Type == RiskType.ConstraintViolation),
            "制約違反が検出されるべき"
        );
    }

    /// <summary>
    /// テスト観点: 循環参照がある場合、異常（Error）として検出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldDetectCycle()
    {
        // Arrange
        var project = new Project();
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        project.AddTask(t1);
        project.AddTask(t2);

        t1.AddConstraint(new TaskConstraint(t2.Id));
        t2.AddConstraint(new TaskConstraint(t1.Id));

        // Act
        var risks = await _useCase.ExecuteAsync(project);

        // Assert
        Assert.IsTrue(
            risks.Any(r => r.Type == RiskType.CycleDetected && r.IsError),
            "循環参照はエラーとして検出されるべき"
        );
    }

    /// <summary>
    /// テスト観点: 期限を過ぎているタスクがある場合、Overdue リスクとして検出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldDetectOverdue()
    {
        // Arrange
        var project = new Project();
        var task = new ProjectTask { Id = Guid.NewGuid() };
        task.UpdateSchedule(null, DateTime.Today.AddDays(-1)); // 期限切れ
        task.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress);
        project.AddTask(task);

        // Act
        var risks = await _useCase.ExecuteAsync(project);

        // Assert
        Assert.IsTrue(risks.Any(r => r.TaskId == task.Id && r.Type == RiskType.Overdue), "期限切れが検出されるべき");
    }

    /// <summary>
    /// テスト観点: SS (Start-to-Start) 制約において、先行タスクが未着手なのに後続タスクが開始されている場合、違反として検出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldDetectSSViolation()
    {
        // Arrange
        var project = new Project();
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        t1.UpdateName("Pre-SS");
        t1.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.NotStarted); // 未着手

        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        t2.UpdateName("Succ-SS");
        t2.AddConstraint(new TaskConstraint(t1.Id, TaskConstraintType.SS));
        t2.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress); // 先行未着手なのに開始

        project.AddTask(t1);
        project.AddTask(t2);

        // Act
        var risks = await _useCase.ExecuteAsync(project);

        // Assert
        // 現状の実装は FS のみだが、テストを追加して将来の対応を促す（または実装する）
        // 40.2 の Green で FS 以外も実装する方針
        Assert.IsTrue(risks.Any(r => r.TaskId == t2.Id && r.Type == RiskType.ConstraintViolation));
    }

    /// <summary>
    /// テスト観点: FF (Finish-to-Finish) 制約において、先行タスクが未完了なのに後続タスクが完了している場合、違反として検出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldDetectFFViolation()
    {
        // Arrange
        var project = new Project();
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        t1.UpdateName("Pre-FF");
        t1.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress); // 未完了

        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        t2.UpdateName("Succ-FF");
        t2.AddConstraint(new TaskConstraint(t1.Id, TaskConstraintType.FF));
        t2.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.Completed); // 先行未了なのに完了

        project.AddTask(t1);
        project.AddTask(t2);

        // Act
        var risks = await _useCase.ExecuteAsync(project);

        // Assert
        Assert.IsTrue(risks.Any(r => r.TaskId == t2.Id && r.Type == RiskType.ConstraintViolation));
    }
}
