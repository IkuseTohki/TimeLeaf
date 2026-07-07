using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LeafKit.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class CommentFlowTests
{
    private MainViewModel _mainViewModel = null!;
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IOSNotificationService> _osNotificationServiceMock = null!;
    private Mock<ICheckAssignmentUseCase> _checkAssignmentMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private ProjectViewModel _projectViewModel = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _dialogServiceMock = new Mock<IDialogService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _osNotificationServiceMock = new Mock<IOSNotificationService>();
        _checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        _userServiceMock = new Mock<IUserService>();

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();

        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        var task = new ProjectTask();
        task.UpdateName("Test Task");
        project.AddTask(task);

        // Factory mock setup (CreateProjectViewModel の前にセットアップが必要)
        viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        _projectViewModel = new ProjectViewModel(
            project,
            _testUserId,
            new Mock<IJoinProjectUseCase>().Object,
            viewModelFactoryMock.Object
        );

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new[] { project });

        var addTaskUseCase = new AddTaskUseCase(_saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(_saveUseCaseMock.Object, _identityServiceMock.Object);

        // ViewModel 自身のファクトリ戻り値設定
        viewModelFactoryMock
            .Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns(
                (Project p) =>
                    new ProjectViewModel(
                        p,
                        _testUserId,
                        new Mock<IJoinProjectUseCase>().Object,
                        viewModelFactoryMock.Object
                    )
            );

        var saveCoordinator = new ProjectSaveCoordinator(
            _saveUseCaseMock.Object,
            new Mock<ILogger<ProjectSaveCoordinator>>().Object
        );
        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock
            .Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var snackbarServiceMock = new Mock<ISnackbarService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();
        var settingsRepoMock = new Mock<IApplicationSettingsRepository>();
        var settings = new ApplicationSettings();

        _mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            _saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            projectServiceMock.Object,
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
            _userServiceMock.Object,
            settingsRepoMock.Object,
            settings,
            loggerMock.Object
        );

        viewModelFactoryMock
            .Setup(x =>
                x.CreateProjectWorkspaceViewModel(
                    It.IsAny<ProjectViewModel>(),
                    It.IsAny<ObservableCollection<ProjectViewModel>>()
                )
            )
            .Returns(
                (ProjectViewModel pvm, ObservableCollection<ProjectViewModel> projects) =>
                    new ProjectWorkspaceViewModel(
                        pvm,
                        projects,
                        _notificationServiceMock.Object,
                        viewModelFactoryMock.Object,
                        _checkAssignmentMock.Object,
                        _loggerMock.Object
                    )
            );
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
        var addCommentUseCase = new AddCommentUseCase(_saveUseCaseMock.Object, _identityServiceMock.Object);

        var historyUseCaseMock = new Mock<IGetTaskHistoryUseCase>();
        historyUseCaseMock
            .Setup(x => x.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(Enumerable.Empty<ChangeRecord>());

        var detailVM = new TaskDetailViewModel(
            projectVM,
            taskVM,
            addCommentUseCase,
            new Mock<ISaveProjectUseCase>().Object,
            new Mock<IDeleteTaskUseCase>().Object,
            new Mock<IDialogService>().Object,
            _userServiceMock.Object,
            new Mock<IProjectService>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object,
            new Mock<IGetProjectMembersUseCase>().Object,
            historyUseCaseMock.Object
        );

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

    /// <summary>
    /// テスト観点: コレクションのリセット（同期など）が発生した後も、タスクの選択状態が維持されることを確認する。
    /// </summary>
    [TestMethod]
    public void TaskSelection_ShouldBePreserved_AfterCollectionReset()
    {
        // Arrange
        var projectVM = _mainViewModel.Projects.First();
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            new Mock<IAddTaskUseCase>().Object,
            new Mock<IAddContainerUseCase>().Object,
            new Mock<IMoveTaskUseCase>().Object,
            new Mock<IDeleteTaskUseCase>().Object,
            new Mock<IDeleteContainerUseCase>().Object,
            new Mock<IGetProjectMembersUseCase>().Object,
            new Mock<ISaveProjectUseCase>().Object,
            new Mock<IViewModelFactory>().Object,
            new Mock<IDialogService>().Object,
            new DetectProjectRisksUseCase(new CalculateCriticalPathUseCase()),
            new CalculateFlowLayoutUseCase(),
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object
        );

        var targetTask = projectVM.Tasks.First();
        tasksVM.SelectedTask = targetTask;

        // Act
        projectVM.SyncFromModel();

        // Assert
        Assert.IsNotNull(tasksVM.SelectedTask, "コレクションリセット後もタスクが選択されていること");
        Assert.AreEqual(targetTask.Id, tasksVM.SelectedTask.Id, "選択されていたタスクのIDが一致すること");
    }
}
