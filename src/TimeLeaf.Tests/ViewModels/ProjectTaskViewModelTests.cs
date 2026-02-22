using System;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectTaskViewModelTests
{
    // [TestMethod]
    // public void Constructor_ShouldThrowArgumentNullException_WhenProjectTaskIsNull()
    // {
    //     // Arrange
    //     ProjectTask projectTask = null!;
    //
    //     // Act & Assert
    //     Assert.ThrowsException<ArgumentNullException>(() => new ProjectTaskViewModel(projectTask), "null ProjectTask でコンストラクタを呼び出した際にArgumentNullExceptionがスローされること");
    // }

    /// <summary>
    /// テスト観点: Name プロパティを変更した際に、基になる ProjectTask エンティティの Name が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Name_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var projectTask = new ProjectTask { Name = "Old Task Name" };
        var viewModel = new ProjectTaskViewModel(projectTask);
        var newName = "New Task Name";

        var receivedEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectTaskViewModel.Name))
            {
                receivedEvents++;
            }
        };

        // Act
        viewModel.Name = newName;

        // Assert
        Assert.AreEqual(newName, projectTask.Name, "基になるProjectTaskエンティティのNameが更新されること");
        Assert.AreEqual(1, receivedEvents, "Nameプロパティの変更時にPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: EstimatedCost プロパティを変更した際に、基になる ProjectTask エンティティの EstimatedCost が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void EstimatedCost_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var projectTask = new ProjectTask { EstimatedCost = 10.0 };
        var viewModel = new ProjectTaskViewModel(projectTask);
        var newCost = 15.5;

        var receivedEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectTaskViewModel.EstimatedCost))
            {
                receivedEvents++;
            }
        };

        // Act
        viewModel.EstimatedCost = newCost;

        // Assert
        Assert.AreEqual(newCost, projectTask.EstimatedCost, "基になるProjectTaskエンティティのEstimatedCostが更新されること");
        Assert.AreEqual(1, receivedEvents, "EstimatedCostプロパティの変更時にPropertyChangedイベントが発火すること");
    }
}
