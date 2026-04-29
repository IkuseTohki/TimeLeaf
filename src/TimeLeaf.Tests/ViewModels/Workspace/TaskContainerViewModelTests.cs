using System;
using System.Collections.ObjectModel;
using System.Linq;
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
    private Mock<TimeLeaf.Services.IUserService> _userServiceMock = null!;
    private Mock<IRelayCommand> _commandMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _userServiceMock = new Mock<TimeLeaf.Services.IUserService>();
        _commandMock = new Mock<IRelayCommand>();
    }

    /// <summary>
    /// テスト観点: 親タスクViewModelと子タスクViewModelのリストが正しく保持されることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetParentAndChildren()
    {
        // Arrange
        var parentTask = new ProjectTask();
        parentTask.UpdateName("Parent Task");
        var parentVm = new ProjectTaskViewModel(parentTask, _userServiceMock.Object);

        var child1 = new ProjectTask();
        var child2 = new ProjectTask();
        parentTask.AddChild(child1);
        parentTask.AddChild(child2);

        var childVms = new ObservableCollection<ProjectTaskViewModel>
        {
            new ProjectTaskViewModel(child1, _userServiceMock.Object),
            new ProjectTaskViewModel(child2, _userServiceMock.Object),
        };

        // Act
        var containerVm = new TaskContainerViewModel(parentVm, childVms, _commandMock.Object);

        // Assert
        Assert.AreEqual(parentVm, containerVm.ParentTask);
        Assert.AreEqual(2, containerVm.SubTasks.Count);
        Assert.AreEqual("Parent Task", containerVm.DisplayName);
    }

    /// <summary>
    /// テスト観点: 親タスクがnullの場合（未分類コンテナ）の動作を確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_WithNullParent_ShouldRepresentUnclassified()
    {
        // Act
        var containerVm = new TaskContainerViewModel(
            null,
            new ObservableCollection<ProjectTaskViewModel>(),
            _commandMock.Object
        );

        // Assert
        Assert.IsNull(containerVm.ParentTask);
        Assert.AreEqual("未分類のタスク", containerVm.DisplayName);
        Assert.IsTrue(containerVm.IsUnclassified);
    }
}
