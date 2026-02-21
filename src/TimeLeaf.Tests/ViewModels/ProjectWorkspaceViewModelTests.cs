using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        var viewModel = new ProjectWorkspaceViewModel(project);
        var taskName = "New Task";
        viewModel.NewTaskName = taskName;

        // Act
        viewModel.AddTaskCommand.Execute(null);

        // Assert
        Assert.HasCount(1, viewModel.Tasks);
        Assert.AreEqual(taskName, viewModel.Tasks.First().Name);
        Assert.AreEqual(string.Empty, viewModel.NewTaskName, "追加後は入力欄がクリアされること");
    }
}
