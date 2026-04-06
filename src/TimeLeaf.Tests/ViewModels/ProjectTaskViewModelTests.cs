using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.ViewModels;
using TimeLeaf.Services;
using TimeLeaf.Repositories;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectTaskViewModelTests
{
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();
    }

    /// <summary>
    /// テスト観点: Name プロパティを変更した際に、基になる ProjectTask エンティティの Name が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Name_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var projectTask = new ProjectTask();
        projectTask.UpdateName("Old Task Name");
        var viewModel = new ProjectTaskViewModel(projectTask, _userServiceMock.Object);
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
        var projectTask = new ProjectTask();
        projectTask.UpdateEstimatedCost(10.0);
        var viewModel = new ProjectTaskViewModel(projectTask, _userServiceMock.Object);
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

    /// <summary>
    /// テスト観点: PriorityValues および StatusValues が、対応する列挙型のすべての値を返していることを確認する。
    /// これにより、ComboBox 等の選択肢が正しく UI に提供されることを保証する。
    /// </summary>
    [TestMethod]
    public void EnumValues_ShouldReturnAllOptions()
    {
        // Arrange
        var projectTask = new ProjectTask();
        var viewModel = new ProjectTaskViewModel(projectTask, _userServiceMock.Object);

        // Act & Assert
        var expectedPriorities = Enum.GetValues(typeof(TaskPriority));
        CollectionAssert.AreEquivalent(expectedPriorities, viewModel.PriorityValues.ToArray(), "PriorityValues がすべての優先度を返すこと");

        var expectedStatuses = Enum.GetValues(typeof(TimeLeaf.Models.Enums.TaskStatus));
        CollectionAssert.AreEquivalent(expectedStatuses, viewModel.StatusValues.ToArray(), "StatusValues がすべてのステータスを返すこと");
    }

    /// <summary>
    /// テスト観点: Description プロパティを変更した際に、モデルが更新され、イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Description_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var projectTask = new ProjectTask();
        var viewModel = new ProjectTaskViewModel(projectTask, _userServiceMock.Object);
        var newDesc = "Updated Description";
        var received = false;
        viewModel.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(viewModel.Description)) received = true; };

        // Act
        viewModel.Description = newDesc;

        // Assert
        Assert.AreEqual(newDesc, projectTask.Description);
        Assert.IsTrue(received);
    }

    /// <summary>
    /// テスト観点: Status プロパティを変更した際に、モデルが更新され、関連するプロパティ（日付等）の通知も行われることを確認する。
    /// </summary>
    [TestMethod]
    public void Status_ShouldUpdateModelAndRaiseMultipleNotifications()
    {
        // Arrange
        var projectTask = new ProjectTask();
        var viewModel = new ProjectTaskViewModel(projectTask, _userServiceMock.Object);
        var newStatus = TimeLeaf.Models.Enums.TaskStatus.Completed;
        var propertyNames = new List<string>();
        viewModel.PropertyChanged += (s, e) => propertyNames.Add(e.PropertyName!);

        // Act
        viewModel.Status = newStatus;

        // Assert
        Assert.AreEqual(newStatus, projectTask.Status);
        CollectionAssert.Contains(propertyNames, nameof(viewModel.Status));
        CollectionAssert.Contains(propertyNames, nameof(viewModel.ActualStartDate));
        CollectionAssert.Contains(propertyNames, nameof(viewModel.ActualEndDate));
    }

    /// <summary>
    /// テスト観点: UpdateFromModel を呼び出した際に、すべてのプロパティが最新のモデル状態と同期され、通知が行われることを確認する。
    /// </summary>
    [TestMethod]
    public void UpdateFromModel_ShouldSyncAllProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var oldTask = new ProjectTask { Id = id };
        var viewModel = new ProjectTaskViewModel(oldTask, _userServiceMock.Object);

        var newTask = new ProjectTask { Id = id };
        newTask.UpdateName("Updated Name");
        newTask.UpdatePriority(TaskPriority.High);
        newTask.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress);

        var notifiedProperties = new List<string>();
        viewModel.PropertyChanged += (s, e) => notifiedProperties.Add(e.PropertyName!);

        // Act
        viewModel.UpdateFromModel(newTask);

        // Assert
        Assert.AreEqual("Updated Name", viewModel.Name);
        Assert.AreEqual(TaskPriority.High, viewModel.Priority);
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.InProgress, viewModel.Status);

        // 多数のプロパティが通知されるはず
        Assert.IsTrue(notifiedProperties.Count >= 10);
        CollectionAssert.Contains(notifiedProperties, nameof(viewModel.Name));
        CollectionAssert.Contains(notifiedProperties, nameof(viewModel.Priority));
        CollectionAssert.Contains(notifiedProperties, nameof(viewModel.Status));
    }
}
