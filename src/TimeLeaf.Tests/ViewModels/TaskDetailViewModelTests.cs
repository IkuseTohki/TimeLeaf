using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    private Mock<IProjectService> _projectServiceMock = null!;
    private Mock<IJoinProjectUseCase> _joinProjectUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IGetProjectMembersUseCase> _getProjectMembersUseCaseMock = null!;
    private Mock<IGetTaskHistoryUseCase> _getTaskHistoryUseCaseMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;

    private TaskDetailViewModel _viewModel = null!;
    private ProjectViewModel _projectViewModel = null!;
    private ProjectTaskViewModel _taskViewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        _saveProjectUseCaseMock = new Mock<ISaveProjectUseCase>();
        _deleteTaskUseCaseMock = new Mock<IDeleteTaskUseCase>();
        _dialogServiceMock = new Mock<IDialogService>();
        _userServiceMock = new Mock<IUserService>();
        _projectServiceMock = new Mock<IProjectService>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _getProjectMembersUseCaseMock = new Mock<IGetProjectMembersUseCase>();
        _getTaskHistoryUseCaseMock = new Mock<IGetTaskHistoryUseCase>();
        _loggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        _joinProjectUseCaseMock = new Mock<IJoinProjectUseCase>();
        _identityServiceMock = new Mock<IIdentityService>();

        _getTaskHistoryUseCaseMock = new Mock<IGetTaskHistoryUseCase>();
        _loggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        _joinProjectUseCaseMock = new Mock<IJoinProjectUseCase>();

        // Setup TaskHistory mock to return empty list by default
        _getTaskHistoryUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<ChangeRecord>());

        var project = new Project(Guid.NewGuid());
        project.UpdateName("Test Project");

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
            null,
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
            _loggerMock.Object,
            _getProjectMembersUseCaseMock.Object,
            _getTaskHistoryUseCaseMock.Object,
            _identityServiceMock.Object
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
    public void ProjectUpdated_WithExternalNameChange_ShouldSetHasExternalChange()
    {
        // テスト観点: 他ユーザーがタスク名を変更した場合、HasExternalChange が true になることを確認する。
        // 実際の動作では ProjectService がストレージから新しい Project オブジェクトを読み込み、
        // そのオブジェクトを ProjectUpdated イベントで通知する。ここではその挙動を再現する。

        // Arrange: タスク名が変更された「ストレージ再ロード済み」のプロジェクトを別オブジェクトとして用意する
        var updatedTask = _taskViewModel.Model.Clone();
        updatedTask.UpdateName("External Changed Name");

        var updatedProject = new Project(
            _projectViewModel.Model.Id,
            _projectViewModel.Model.Name,
            _projectViewModel.Model.Description,
            _projectViewModel.Model.Status,
            _projectViewModel.Model.CreatedAt,
            _projectViewModel.Model.UpdatedAt,
            _projectViewModel.Model.CreatedBy,
            new List<ProjectTask> { updatedTask },
            _projectViewModel.Model.Containers.ToList(),
            _projectViewModel.Model.Milestones.ToList(),
            _projectViewModel.Model.AssignedUserIds.ToList()
        );

        // Act: 外部変更イベントを再ロードされた別オブジェクトで発火（ProjectService の実際の動作を再現）
        _projectServiceMock.Raise(x => x.ProjectUpdated += null, updatedProject);

        // Assert
        Assert.IsTrue(
            _viewModel.HasExternalChange,
            "他ユーザーがタスク名を変更した場合 HasExternalChange が true になること"
        );
    }

    [TestMethod]
    public void ProjectUpdated_WithNoActualChange_ShouldNotSetHasExternalChange()
    {
        // テスト観点: プロジェクト更新イベントが来ても、実質的な差分がない場合は HasExternalChange が true にならないことを確認する。

        // Arrange: 変更なしの「ストレージ再ロード済み」プロジェクトを別オブジェクトとして用意する
        var unchangedTask = _taskViewModel.Model.Clone();

        var unchangedProject = new Project(
            _projectViewModel.Model.Id,
            _projectViewModel.Model.Name,
            _projectViewModel.Model.Description,
            _projectViewModel.Model.Status,
            _projectViewModel.Model.CreatedAt,
            _projectViewModel.Model.UpdatedAt,
            _projectViewModel.Model.CreatedBy,
            new List<ProjectTask> { unchangedTask },
            _projectViewModel.Model.Containers.ToList(),
            _projectViewModel.Model.Milestones.ToList(),
            _projectViewModel.Model.AssignedUserIds.ToList()
        );

        // Act: 変更なしで外部変更イベントを発火
        _projectServiceMock.Raise(x => x.ProjectUpdated += null, unchangedProject);

        // Assert
        Assert.IsFalse(_viewModel.HasExternalChange, "実質的な変更がない場合 HasExternalChange が true にならないこと");
    }

    [TestMethod]
    public async Task DeleteCommand_ShouldConfirmAndDelete()
    {
        // Arrange
        _dialogServiceMock.Setup(x => x.ShowConfirmationDialog(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

        // Act
        await _viewModel.DeleteCommand.ExecuteAsync(null);

        // Assert
        _deleteTaskUseCaseMock.Verify(x => x.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Id), Times.Once);
        // Note: DeleteCommand execution typically triggers RequestClose via UI navigation logic in real usage,
        // but here we only verify the UseCase call.
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

        System.Diagnostics.Debug.WriteLine($"NewCommentContent: '{_viewModel.NewCommentContent}'");
        Assert.IsTrue(
            string.IsNullOrWhiteSpace(_viewModel.NewCommentContent),
            $"入力欄がクリアされていること。実際の内容: '{_viewModel.NewCommentContent}'"
        );
    }

    [TestMethod]
    public async Task AddCommentCommand_CanExecute_ShouldReturnFalse_WhenContentIsEmpty()
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

    /// <summary>
    /// テスト観点: 担当者選択用のメンバーリストが正しく取得されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task LoadMembers_ShouldFetchFromUseCase()
    {
        // Arrange
        var members = new List<User> { new User(Guid.NewGuid(), "User1", "#000", "") };
        _getProjectMembersUseCaseMock.Setup(x => x.ExecuteAsync(_projectViewModel.Model)).ReturnsAsync(members);

        var viewModel = new TaskDetailViewModel(
            _projectViewModel,
            _taskViewModel,
            _addCommentUseCaseMock.Object,
            _saveProjectUseCaseMock.Object,
            _deleteTaskUseCaseMock.Object,
            _dialogServiceMock.Object,
            _userServiceMock.Object,
            _projectServiceMock.Object,
            _loggerMock.Object,
            _getProjectMembersUseCaseMock.Object,
            _getTaskHistoryUseCaseMock.Object,
            _identityServiceMock.Object
        );

        // Act
        await viewModel.LoadMembersAsync();

        // Assert
        Assert.AreEqual(1, viewModel.AvailableTeammates.Count);
        Assert.AreEqual("User1", viewModel.AvailableTeammates[0].DisplayName);
    }
}
