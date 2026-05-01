using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class CalculateFlowLayoutUseCaseTests
{
    private CalculateFlowLayoutUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _useCase = new CalculateFlowLayoutUseCase();
    }

    /// <summary>
    /// テスト観点: プロジェクトにタスクが含まれる場合、それぞれのタスクに対して
    /// 座標（X, Y）が計算され、マップに格納されることを確認する。
    /// </summary>
    [TestMethod]
    public void Execute_ShouldReturnCoordinatesForEachTask()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task1 = new ProjectTask();
        task1.UpdateName("Task 1");
        var task2 = new ProjectTask();
        task2.UpdateName("Task 2");
        project.AddTask(task1);
        project.AddTask(task2);

        // Act
        var layout = _useCase.Execute(project);

        // Assert
        Assert.AreEqual(2, layout.Count);
        Assert.IsTrue(layout.ContainsKey(task1.Id));
        Assert.IsTrue(layout.ContainsKey(task2.Id));
    }
}
