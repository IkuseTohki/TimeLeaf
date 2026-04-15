using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ViewModelModelSyncTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _userServiceMock = new Mock<IUserService>();

        // ProjectViewModel がタスクを生成する際に使用するファクトリのセットアップ
        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));
    }

    /// <summary>
    /// テスト観点: ProjectViewModel のプロパティを変更した際、
    /// 基になる Project エンティティのプロパティが更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectViewModel_UpdateProperty_ShouldUpdateModel()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Old Name");
        project.UpdateDescription("Old Desc");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Act
        viewModel.Name = "New Name";
        viewModel.Description = "New Desc";

        // Assert
        Assert.AreEqual("New Name", project.Name);
        Assert.AreEqual("New Desc", project.Description);
    }

    /// <summary>
    /// テスト観点: ProjectTaskViewModel のプロパティを変更した際、
    /// 基になる ProjectTask エンティティのプロパティが更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectTaskViewModel_UpdateProperty_ShouldUpdateModel()
    {
        // Arrange
        var task = new ProjectTask();
        task.UpdateName("Old Task");
        task.AssignTo("Old User");
        var viewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);

        // Act
        viewModel.Name = "New Task";
        viewModel.Assignee = "New User";

        // Assert
        Assert.AreEqual("New Task", task.Name);
        Assert.AreEqual("New User", task.Assignee);
    }

    /// <summary>
    /// テスト観点: Project エンティティにタスクを追加し同期した際、
    /// ProjectViewModel の Tasks コレクションにも ViewModel が追加されることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectModel_AddTask_ShouldReflectInViewModel()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var newTask = new ProjectTask();
        newTask.UpdateName("New Task");

        // Act
        project.AddTask(newTask);
        viewModel.SyncFromModel(); // 同期メソッドを呼ぶ

        // Assert
        Assert.IsNotNull(viewModel.Tasks);
        Assert.AreEqual(1, viewModel.Tasks.Count);
        Assert.AreEqual(newTask.Id, viewModel.Tasks.First().Id);
    }

    /// <summary>
    /// テスト観点: Project エンティティからタスクを削除し同期した際、
    /// ProjectViewModel の Tasks コレクションからも ViewModel が削除されることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectModel_RemoveTask_ShouldReflectInViewModel()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");
        var task = new ProjectTask();
        task.UpdateName("Task 1");
        project.AddTask(task);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Act
        project.RemoveTask(task.Id);
        viewModel.SyncFromModel(); // 同期メソッドを呼ぶ

        // Assert
        Assert.IsNotNull(viewModel.Tasks);
        Assert.AreEqual(0, viewModel.Tasks.Count);
    }
}
