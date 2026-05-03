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
        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

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
        project.AddTask(task);
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
    public async Task CloseCommand_WhenNotDirty_ShouldCloseImmediately()
    {
        // Arrange
        bool closeRequested = false;
        _viewModel.RequestClose += (res) => closeRequested = true;

        // Act
        _viewModel.CloseCommand.Execute(null);

        // Assert
        Assert.IsTrue(closeRequested);
        _dialogServiceMock.Verify(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task CloseCommand_WhenDirtyAndCanceled_ShouldNotClose()
    {
        // Arrange
        _viewModel.Task.Name = "Changed Name";
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        bool closeRequested = false;
        _viewModel.RequestClose += (res) => closeRequested = true;

        // Act
        _viewModel.CloseCommand.Execute(null);

        // Assert
        Assert.IsFalse(closeRequested, "キャンセルした場合は閉じられないこと");
    }

    [TestMethod]
    public void ReloadLatest_WhenNotDirty_ShouldSyncFromMaster()
    {
        // Arrange
        // マスターモデルを外部から書き換えられたと仮定
        var latestName = "Latest Name From Other User";
        _taskViewModel.Model.UpdateName(latestName);
        _taskViewModel.UpdateFromModel(_taskViewModel.Model);

        _viewModel.HasExternalChange = true;

        // Act
        _viewModel.ReloadLatestCommand.Execute(null);

        // Assert
        Assert.AreEqual(latestName, _viewModel.Task.Name, "最新のデータが作業コピーに反映されること");
        Assert.IsFalse(_viewModel.HasExternalChange, "リロード後は HasExternalChange が解消されること");
    }

    [TestMethod]
    public void ReloadLatest_WhenDirtyAndConfirmed_ShouldSyncFromMaster()
    {
        // Arrange
        _viewModel.Task.Name = "My Edit";
        var latestName = "Latest Name";
        _taskViewModel.Model.UpdateName(latestName);

        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        _viewModel.ReloadLatestCommand.Execute(null);

        // Assert
        Assert.AreEqual(latestName, _viewModel.Task.Name);
        Assert.IsFalse(_viewModel.IsDirty);
    }

    [TestMethod]
    public async Task SaveCommand_WhenExternalChangeExistsAndConfirmed_ShouldSave()
    {
        // Arrange
        _viewModel.Task.Name = "My Final Version";
        _viewModel.HasExternalChange = true;
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _saveProjectUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<Project>()), Times.Once);
        Assert.IsFalse(_viewModel.HasExternalChange);
    }

    [TestMethod]
    public async Task SaveCommand_WhenExternalChangeExistsAndCanceled_ShouldNotSave()
    {
        // Arrange
        _viewModel.Task.Name = "My Edit";
        _viewModel.HasExternalChange = true;
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        await _viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        _saveProjectUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<Project>()), Times.Never);
        Assert.IsTrue(_viewModel.HasExternalChange);
    }

    [TestMethod]
    public void ProjectUpdated_ForDifferentProject_ShouldNotSetHasExternalChange()
    {
        // Arrange
        var otherProject = new Project(Guid.NewGuid());

        // Act
        _projectServiceMock.Raise(x => x.ProjectUpdated += null, otherProject);

        // Assert
        Assert.IsFalse(_viewModel.HasExternalChange, "別プロジェクトの更新は無視すること");
    }

    [TestMethod]
    public async Task AddComment_ShouldUpdateBothMasterAndWorkingCopy()
    {
        // Arrange
        var content = "New Comment";
        _viewModel.NewCommentContent = content;

        // AddCommentUseCase が呼ばれたらモデルにコメントが追加されるようにモック（または本物）が必要
        // ここでは、ViewModel 内部でモデルに反映されることを期待する
        _addCommentUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<Project>(), It.IsAny<ProjectTask>(), content))
            .Callback<Project, ProjectTask, string>(
                (p, t, c) =>
                {
                    t.AddComment(new Comment(Guid.NewGuid(), t.Id, Guid.NewGuid(), DateTime.Now, c, null));
                }
            )
            .Returns(System.Threading.Tasks.Task.CompletedTask);

        // Act
        await _viewModel.AddCommentCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual(1, _taskViewModel.Model.Comments.Count, "マスターにコメントが追加されていること");
        Assert.AreEqual(1, _viewModel.Task.Comments.Count, "作業用コピーの表示も更新されていること");
        Assert.AreEqual(string.Empty, _viewModel.NewCommentContent, "入力欄がクリアされていること");
    }

    [TestMethod]
    public void AddCommentCommand_CanExecute_ShouldReturnFalse_WhenContentIsEmpty()
    {
        // Arrange
        _viewModel.NewCommentContent = "";

        // Assert
        Assert.IsFalse(_viewModel.AddCommentCommand.CanExecute(null));

        // Act
        _viewModel.NewCommentContent = "Valid";

        // Assert
        Assert.IsTrue(_viewModel.AddCommentCommand.CanExecute(null));
    }
}
