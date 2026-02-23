using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ViewModelModelSyncTests
{
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
        var viewModel = new ProjectViewModel(project);

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
        var task = new ProjectTask { Name = "Old Task", Assignee = "Old User" };
        var viewModel = new ProjectTaskViewModel(task);

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
        var viewModel = new ProjectViewModel(project);
        var newTask = new ProjectTask { Name = "New Task" };

        // Act
        project.AddTask(newTask);
        viewModel.SyncFromModel(); // 同期メソッドを呼ぶ

        // Assert
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
        var task = new ProjectTask { Name = "Task 1" };
        project.AddTask(task);
        var viewModel = new ProjectViewModel(project);

        // Act
        project.RemoveTask(task.Id);
        viewModel.SyncFromModel(); // 同期メソッドを呼ぶ

        // Assert
        Assert.AreEqual(0, viewModel.Tasks.Count);
    }
}
