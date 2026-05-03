using System;
using System.ComponentModel;
using System.Threading.Tasks;
using LeafKit.UI.Services;
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
public class TaskDetailViewModelTests
{
    private Mock<IAddCommentUseCase> _addCommentUseCaseMock = null!;
    private Mock<ISaveProjectUseCase> _saveProjectUseCaseMock = null!;
    private Mock<IDeleteTaskUseCase> _deleteTaskUseCaseMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<ILogger<TaskDetailViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<IJoinProjectUseCase> _joinProjectUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;

    private Mock<IProjectService> _projectServiceMock = null!;

    private ProjectViewModel _projectViewModel = null!;
    private ProjectTaskViewModel _taskViewModel = null!;
    private TaskDetailViewModel _viewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        _saveProjectUseCaseMock = new Mock<ISaveProjectUseCase>();
        _deleteTaskUseCaseMock = new Mock<IDeleteTaskUseCase>();
        _dialogServiceMock = new Mock<IDialogService>();
        _loggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        _userServiceMock = new Mock<IUserService>();
        _joinProjectUseCaseMock = new Mock<IJoinProjectUseCase>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _projectServiceMock = new Mock<IProjectService>();

        var project = new Project(Guid.NewGuid());
        _projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            _joinProjectUseCaseMock.Object,
            _viewModelFactoryMock.Object
        );

        var task = new ProjectTask(
            Guid.NewGuid(),
            "Initial Name",
            "",
            TimeLeaf.Models.Enums.TaskStatus.NotStarted,
            TimeLeaf.Models.Enums.TaskPriority.Medium,
            null,
            null,
            null,
            null,
            0,
            0,
            "",
            null,
            null
        );
        _taskViewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);

        _viewModel = new TaskDetailViewModel(
            _projectViewModel,
            _taskViewModel,
            _addCommentUseCaseMock.Object,
            _saveProjectUseCaseMock.Object,
            _deleteTaskUseCaseMock.Object,
            _dialogServiceMock.Object,
            _userServiceMock.Object,
            _projectServiceMock.Object,
            _loggerMock.Object
        );
    }

    [TestMethod]
    public void InitialState_ShouldNotBeDirty()
    {
        Assert.IsFalse(_viewModel.IsDirty);
    }

    [TestMethod]
    public void PropertyChanged_ShouldMakeItDirty()
    {
        // Act
        // UI がバインドされている Task (Working Copy) を変更する
        _viewModel.Task.Name = "Changed Name";

        // Assert
        Assert.IsTrue(_viewModel.IsDirty, "作業用コピーのプロパティが変更されたら IsDirty が true になること");
        Assert.AreEqual("Initial Name", _taskViewModel.Name, "マスタータスクはまだ変更されていないこと");
    }

    [TestMethod]
    public async Task SaveCommand_ShouldMergeAndCallUseCaseAndResetDirty()
    {
        // Arrange
        _viewModel.Task.Name = "Changed Name";
        Assert.IsTrue(_viewModel.IsDirty);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _saveProjectUseCaseMock.Verify(x => x.ExecuteAsync(_projectViewModel.Model), Times.Once);
        Assert.AreEqual("Changed Name", _taskViewModel.Name, "保存後はマスタータスクに変更が反映されていること");
        Assert.IsFalse(_viewModel.IsDirty, "保存後は IsDirty が false に戻ること");
    }

    [TestMethod]
    public void ProjectUpdated_ShouldSetHasExternalChange()
    {
        // Act
        // 外部変更イベントを発火
        _projectServiceMock.Raise(x => x.ProjectUpdated += null, _projectViewModel.Model);

        // Assert
        Assert.IsTrue(_viewModel.HasExternalChange, "外部変更を検知したら HasExternalChange が true になること");
    }

    [TestMethod]
    public async Task DeleteCommand_ShouldConfirmAndDelete()
    {
        // Arrange
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        bool closeRequested = false;
        _viewModel.RequestClose += (res) => closeRequested = true;

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        _deleteTaskUseCaseMock.Verify(x => x.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Id), Times.Once);
        Assert.IsTrue(closeRequested, "削除後は画面が閉じられること");
    }

    [TestMethod]
    public async Task BackCommand_WhenNotDirty_ShouldCloseImmediately()
    {
        // Arrange
        bool closeRequested = false;
        _viewModel.RequestClose += (res) => closeRequested = true;

        // Act
        await _viewModel.BackCommand.ExecuteAsync(null);

        // Assert
        Assert.IsTrue(closeRequested);
        _dialogServiceMock.Verify(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task BackCommand_WhenDirty_ShouldShowConfirmation()
    {
        // Arrange
        _viewModel.Task.Name = "Changed Name"; // 作業用コピーを変更
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        bool closeRequested = false;
        _viewModel.RequestClose += (res) => closeRequested = true;

        // Act
        await _viewModel.BackCommand.ExecuteAsync(null);

        // Assert
        _dialogServiceMock.Verify(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        Assert.IsTrue(closeRequested);
    }
}
