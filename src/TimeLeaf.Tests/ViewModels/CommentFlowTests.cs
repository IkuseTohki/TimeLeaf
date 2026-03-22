using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class CommentFlowTests
{
    private MainViewModel _mainViewModel = null!;
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IOSNotificationService> _osNotificationServiceMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ICheckAssignmentUseCase> _checkAssignmentMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
        _serviceProviderMock = new Mock<IServiceProvider>();
        _dialogServiceMock = new Mock<IDialogService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _osNotificationServiceMock = new Mock<IOSNotificationService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();

        var project = new Project();
        project.UpdateName("Test Project");
        var task = new ProjectTask();
        task.UpdateName("Test Task");
        project.AddTask(task);
        _projectViewModel = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object);

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new[] { project });

        var addTaskUseCase = new AddTaskUseCase(_saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(_saveUseCaseMock.Object, _userServiceMock.Object);

        // Factory mock setup
        viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object));

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, _dialogServiceMock.Object, viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectWorkspaceViewModel(pvm, _notificationServiceMock.Object, viewModelFactoryMock.Object, _checkAssignmentMock.Object, _loggerMock.Object));

        var saveCoordinator = new ProjectSaveCoordinator(_saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var snackbarServiceMock = new Mock<ISnackbarService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        _mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            _saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            syncServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            _notificationServiceMock.Object,
            snackbarServiceMock.Object,
            _osNotificationServiceMock.Object,
            checkDeadlinesUseCaseMock.Object,
            _dialogServiceMock.Object,
            _identityServiceMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// テスト観点: コメントを追加した際、モデルに反映され、かつリポジトリの保存処理が呼ばれることを確認する。
    /// </summary>
    [TestMethod]
    public async Task AddComment_ShouldTriggerRepositorySave()
    {
        // Arrange
        var projectVM = _mainViewModel.Projects.First();
        var taskVM = projectVM.Tasks.First();
        var addCommentUseCase = new AddCommentUseCase(_saveUseCaseMock.Object, _userServiceMock.Object);

        var detailVM = new TaskDetailViewModel(
            projectVM,
            taskVM,
            addCommentUseCase,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        var commentContent = "New Test Comment";
        detailVM.NewCommentContent = commentContent;

        // Act
        await detailVM.AddCommentCommand.ExecuteAsync(null);

        // Assert
        // 1. モデルにコメントが追加されていること
        Assert.AreEqual(1, taskVM.Model.Comments.Count());
        Assert.AreEqual(commentContent, taskVM.Model.Comments.First().Content);

        // 2. 保存処理が呼ばれていること
        _saveUseCaseMock.Verify(r => r.ExecuteAsync(projectVM.Model), Times.AtLeastOnce);
    }

    [TestMethod]
    public void TaskSelection_ShouldBePreserved_AfterCollectionReset()
    {
        // Arrange
        var projectViewModel = _mainViewModel.Projects.First();
        var addTaskUseCase = new AddTaskUseCase(_saveUseCaseMock.Object);
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        var tasksVM = new ProjectTasksViewModel(
            projectViewModel,
            addTaskUseCase,
            viewModelFactoryMock.Object,
            _dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>())).Returns(tasksVM);
        viewModelFactoryMock.Setup(x => x.CreateTaskSummaryViewModel(It.IsAny<ProjectViewModel>(), It.IsAny<ProjectTaskViewModel>()))
            .Returns((ProjectViewModel pvm, ProjectTaskViewModel tvm) => new TaskSummaryViewModel(pvm, tvm));

        var workspaceViewModel = new ProjectWorkspaceViewModel(projectViewModel, _notificationServiceMock.Object, viewModelFactoryMock.Object, _checkAssignmentMock.Object, _loggerMock.Object);
        workspaceViewModel.SwitchSubViewCommand.Execute("Tasks");

        var targetTask = tasksVM.Tasks.First();
        tasksVM.SelectedTask = targetTask;

        // Act - コレクションのリセットをシミュレート
        projectViewModel.SyncFromModel();

        // Assert
        Assert.IsNotNull(tasksVM.SelectedTask, "コレクションリセット後もタスクが選択されていること");
        Assert.AreEqual(targetTask.Id, tasksVM.SelectedTask.Id, "選択されていたタスクのIDが一致すること");
    }
}
