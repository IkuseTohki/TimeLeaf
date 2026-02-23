using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelTests
{
    /// <summary>
    /// テスト観点: タスク名を入力して追加コマンドを実行した際、プロジェクトにタスクが追加されることを確認する。
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldAddTaskToProject()
    {
        // Arrange
        var project = new Project { Name = "Test Project" };
        var projectViewModel = new ProjectViewModel(project);
        var loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        var viewModel = new ProjectWorkspaceViewModel(projectViewModel, loggerMock.Object);
        var taskName = "New Task";
        var taskDesc = "New Description";
        viewModel.NewTaskName = taskName;
        viewModel.NewTaskDescription = taskDesc;

        // Act
        viewModel.AddTaskCommand.Execute(null);

        // Assert
        Assert.AreEqual(1, viewModel.Tasks.Count);
        var added = viewModel.Tasks.First();
        Assert.AreEqual(taskName, added.Name);
        Assert.AreEqual(taskDesc, added.Description);
        Assert.AreEqual(string.Empty, viewModel.NewTaskName);
        Assert.AreEqual(string.Empty, viewModel.NewTaskDescription);
    }
}
