using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;
using TimeLeaf.Services;
using TimeLeaf.Repositories;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectTasksViewModelTests
{
    private Mock<IAddTaskUseCase> _addTaskUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<ILogger<ProjectTasksViewModel>> _loggerMock = null!;
    private Mock<ILogger<TaskDetailViewModel>> _detailLoggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _addTaskUseCaseMock = new Mock<IAddTaskUseCase>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _userServiceMock = new Mock<IUserService>();

        _viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object));

        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<ProjectTasksViewModel>>();
        _detailLoggerMock = new Mock<ILogger<TaskDetailViewModel>>();

        var project = new Project();
        _projectViewModel = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);
    }

    /// <summary>
    /// テスト観点: タスクのダブルクリック時に実行される OpenTaskDetailWindowCommand が、
    /// 直接ウィンドウを開くのではなく、親 ViewModel に詳細表示を依頼するイベントを発行することを確認する。
    /// これにより、メインウィンドウ内での詳細表示への切り替えを検証する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task OpenTaskDetailWindowCommand_ShouldRaiseTaskDetailRequested()
    {
        // Arrange
        var task = new ProjectTask();
        task.UpdateName("Test Task");
        var taskVm = new ProjectTaskViewModel(task, _userServiceMock.Object);
        ProjectTaskViewModel? requestedTask = null;

        var vm = new ProjectTasksViewModel(
            _projectViewModel,
            _addTaskUseCaseMock.Object,
            _viewModelFactoryMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object,
            _detailLoggerMock.Object);

        vm.TaskDetailRequested += (s, e) => requestedTask = e;

        // Act
        vm.OpenTaskDetailWindowCommand.Execute(taskVm);

        // Assert
        Assert.AreEqual(taskVm, requestedTask, "ダブルクリック時に TaskDetailRequested イベントが適切なタスクで発行されること");

        // 既存のウィンドウ表示は行われないことを確認（オプション）
        _dialogServiceMock.Verify(
            x => x.ShowDialogAsync(It.IsAny<TaskDetailViewModel>()),
            Times.Never,
            "メインウィンドウ内表示に切り替えたため、ダイアログサービスは呼ばれないこと");
    }
}

