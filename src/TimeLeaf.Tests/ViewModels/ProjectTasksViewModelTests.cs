using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectTasksViewModelTests
{
    private Mock<IAddTaskUseCase> _addTaskUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<ILogger<ProjectTasksViewModel>> _loggerMock = null!;
    private Mock<ILogger<TaskDetailViewModel>> _detailLoggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _addTaskUseCaseMock = new Mock<IAddTaskUseCase>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<ProjectTasksViewModel>>();
        _detailLoggerMock = new Mock<ILogger<TaskDetailViewModel>>();

        var project = new Project();
        _projectViewModel = new ProjectViewModel(project);
    }

    /// <summary>
    /// テスト観点: タスクのダブルクリック時に実行される OpenTaskDetailWindowCommand が、
    /// IDialogService を介して適切な TaskDetailViewModel を引数に呼び出されることを確認する。
    /// これにより、別ウィンドウでの詳細表示機能の導線を検証する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task OpenTaskDetailWindowCommand_ShouldCallDialogService()
    {
        // Arrange
        var task = new ProjectTask();
        task.UpdateName("Test Task");
        var taskVm = new ProjectTaskViewModel(task);

        var detailVm = new TaskDetailViewModel(
            _projectViewModel,
            taskVm,
            new Mock<IAddCommentUseCase>().Object,
            _detailLoggerMock.Object);

        _viewModelFactoryMock.Setup(x => x.CreateTaskDetailViewModel(_projectViewModel, taskVm))
            .Returns(detailVm);

        var vm = new ProjectTasksViewModel(
            _projectViewModel,
            _addTaskUseCaseMock.Object,
            _viewModelFactoryMock.Object,
            _dialogServiceMock.Object,
            _loggerMock.Object,
            _detailLoggerMock.Object);

        // Act
        await vm.OpenTaskDetailWindowCommand.ExecuteAsync(taskVm);

        // Assert
        _dialogServiceMock.Verify(
            x => x.ShowDialogAsync(It.Is<TaskDetailViewModel>(v => v.Task == taskVm)),
            Times.Once,
            "ダブルクリックコマンド実行時にDialogService経由で詳細ウィンドウが開かれること");
    }
}
