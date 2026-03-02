using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelTests
{
    /// <summary>
    /// テスト観点: タスク追加コマンドを実行した際、サブビューである ProjectTasksViewModel を通じて
    /// プロジェクトにタスクが追加されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddTask_ShouldAddTaskToProject_ViaSubViewModel()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project);
        var userServiceMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        var tasksLoggerMock = new Mock<ILogger<ProjectTasksViewModel>>();
        var detailLoggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(saveUseCaseMock.Object, userServiceMock.Object);
        var addMilestoneUseCase = new AddMilestoneUseCase(saveUseCaseMock.Object);
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var dialogServiceMock = new Mock<LeafKit.UI.Services.IDialogService>();

        var taskName = "New Task";
        var addTaskViewModel = new AddTaskViewModel { Name = taskName };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel()).Returns(addTaskViewModel);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel)).ReturnsAsync(true);

        var tasksViewModel = new ProjectTasksViewModel(
            projectViewModel,
            addTaskUseCase,
            addCommentUseCase,
            viewModelFactoryMock.Object,
            dialogServiceMock.Object,
            tasksLoggerMock.Object,
            detailLoggerMock.Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(new ProjectDashboardViewModel(projectViewModel, addMilestoneUseCase, new Mock<ILogger<ProjectDashboardViewModel>>().Object));
        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(tasksViewModel);

        var viewModel = new ProjectWorkspaceViewModel(projectViewModel, viewModelFactoryMock.Object, loggerMock.Object);

        // Tasks ビューに切り替え
        viewModel.SwitchSubViewCommand.Execute("Tasks");
        var activeTasksViewModel = (ProjectTasksViewModel)viewModel.CurrentSubViewModel;

        // Act
        await activeTasksViewModel.AddTaskCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual(1, projectViewModel.Tasks.Count);
        var added = projectViewModel.Tasks.First();
        Assert.AreEqual(taskName, added.Name);
    }
}
