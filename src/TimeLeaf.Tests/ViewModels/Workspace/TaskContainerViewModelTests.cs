using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels.Workspace;

[TestClass]
public class TaskContainerViewModelTests
{
    /// <summary>
    /// テスト観点: コンテナ・エンティティを渡した場合、その名前が DisplayName に反映されることを確認する。
    /// </summary>
    [TestMethod]
    public void DisplayName_WithContainerEntity_ShouldReturnContainerName()
    {
        // Arrange
        var container = new ProjectContainer(Guid.NewGuid(), "Feature A");
        var subTasks = new ObservableCollection<ProjectTaskViewModel>();
        var mockCommand = new Mock<IRelayCommand>();

        // Act
        var vm = new TaskContainerViewModel(container, subTasks, mockCommand.Object);

        // Assert
        Assert.AreEqual("Feature A", vm.DisplayName);
        Assert.IsFalse(vm.IsUnclassified);
    }

    /// <summary>
    /// テスト観点: コンテナが null の場合（未分類）、"未分類のタスク" という固定名称になることを確認する。
    /// </summary>
    [TestMethod]
    public void DisplayName_WithoutContainer_ShouldReturnUnclassified()
    {
        // Arrange
        var subTasks = new ObservableCollection<ProjectTaskViewModel>();
        var mockCommand = new Mock<IRelayCommand>();

        // Act
        var vm = new TaskContainerViewModel(null, subTasks, mockCommand.Object);

        // Assert
        Assert.AreEqual("未分類のタスク", vm.DisplayName);
        Assert.IsTrue(vm.IsUnclassified);
    }

    /// <summary>
    /// テスト観点: サブタスクが空の状態でも、集計プロパティがエラーにならず 0 を返すことを確認する。
    /// </summary>
    [TestMethod]
    public void TaskCounts_WithEmptySubTasks_ShouldReturnZero()
    {
        // Arrange
        var container = new ProjectContainer(Guid.NewGuid(), "Empty Container");
        var subTasks = new ObservableCollection<ProjectTaskViewModel>();
        var mockCommand = new Mock<IRelayCommand>();

        // Act
        var vm = new TaskContainerViewModel(container, subTasks, mockCommand.Object);

        // Assert
        Assert.AreEqual(0, vm.TotalTasksCount);
        Assert.AreEqual(0, vm.DoneTasksCount);
    }
}
