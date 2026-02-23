using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class DoubleTaskDisplayReproductionTests
{
    /// <summary>
    /// テスト観点: タスクを追加した際に、ViewModelのTasksコレクションに重複して追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldAddOnlyOneViewModel()
    {
        // Arrange
        var project = new Project { Name = "Test Project" };
        var projectViewModel = new ProjectViewModel(project);
        var loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        var workspaceViewModel = new ProjectWorkspaceViewModel(projectViewModel, loggerMock.Object);

        // Act
        workspaceViewModel.NewTaskName = "New Task";
        workspaceViewModel.AddTaskCommand.Execute(null);

        // Assert
        // Modelのタスク数は1であるべき
        Assert.HasCount(1, project.Tasks, "Modelのタスク数が1であること");

        // ViewModel (ProjectViewModel) のタスク数も1であるべき
        Assert.HasCount(1, projectViewModel.Tasks, "ProjectViewModelのタスク数が1であること");

        // WorkspaceViewModel のタスク数（ProjectViewModel.Tasksへの参照）も1であるべき
        Assert.HasCount(1, workspaceViewModel.Tasks, "ProjectWorkspaceViewModelのタスク数が1であること");
    }

    /// <summary>
    /// テスト観点: Model.Tasks.Clear() (Resetアクション) が発生した際に、
    /// ViewModelのTasksコレクションも正しくクリアされることを確認する。
    /// </summary>
    [TestMethod]
    public void ModelClear_ShouldClearViewModelTasks()
    {
        // Arrange
        var project = new Project { Name = "Test Project" };
        var projectViewModel = new ProjectViewModel(project);
        project.Tasks.Add(new ProjectTask { Name = "Existing Task" });

        // この時点で ViewModel.Tasks には1つ入っているはず
        Assert.HasCount(1, projectViewModel.Tasks, "初期状態でVMのタスクが1つであること");

        // Act
        // Clear() は NotifyCollectionChangedAction.Reset を発生させる
        project.Tasks.Clear();

        // Assert
        Assert.IsEmpty(project.Tasks, "Modelのタスクがクリアされていること");
        Assert.IsEmpty(projectViewModel.Tasks, "Model.Clear() 後に ViewModel のタスクもクリアされていること");
    }
}
