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
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelSaveTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ILogger<MainViewModel>> _mainLoggerMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _workspaceLoggerMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _mainLoggerMock = new Mock<ILogger<MainViewModel>>();
        _workspaceLoggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
        _userServiceMock = new Mock<IUserService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _dialogServiceMock = new Mock<IDialogService>();

        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(_workspaceLoggerMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IIdentityService))).Returns(_identityServiceMock.Object);
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(new Mock<IAddProjectUseCase>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService))).Returns(_dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider))).Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// テスト観点: WorkspaceViewModel でタスクを追加した際に、MainViewModel の自動保存ロジックが走り、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddTaskInWorkspace_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("SaveTest");

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

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
        viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock
            .Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var saveCoordinator = new ProjectSaveCoordinator(
            saveUseCaseMock.Object,
            new Mock<ILogger<ProjectSaveCoordinator>>().Object
        );
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();
        var checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();

        var mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            projectServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            notificationServiceMock.Object,
            snackbarServiceMock.Object,
            osNotificationServiceMock.Object,
            checkDeadlinesUseCaseMock.Object,
            _dialogServiceMock.Object,
            _identityServiceMock.Object,
            _mainLoggerMock.Object
        );
        await System.Threading.Tasks.Task.Delay(100); // InitializeAsync の完了を待つ

        // MainViewModel.Projects から該当の ViewModel を取得
        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);

        // MainViewModel から WorkspaceViewModel へ遷移したと仮定
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);

        var workspaceViewModel = new ProjectWorkspaceViewModel(
            projectViewModel,
            mainViewModel.Projects,
            notificationServiceMock.Object,
            viewModelFactoryMock.Object,
            checkAssignmentMock.Object,
            new Mock<ILogger<ProjectWorkspaceViewModel>>().Object
        );
        var tasksViewModel = new ProjectTasksViewModel(
            projectViewModel,
            addTaskUseCase,
            new Mock<IGetProjectMembersUseCase>().Object,
            viewModelFactoryMock.Object,
            _dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object
        );

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectViewModel)).Returns(tasksViewModel);
        workspaceViewModel.SwitchSubViewCommand.Execute("Tasks");

        // Act
        var taskName = "New Task to Save";
        var addTaskViewModel = new AddTaskViewModel { Name = taskName };
        viewModelFactoryMock
            .Setup(x => x.CreateAddTaskViewModel(It.IsAny<IEnumerable<User>>()))
            .Returns(addTaskViewModel);
        _dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel)).ReturnsAsync(true);

        await tasksViewModel.AddTaskCommand.ExecuteAsync(null);

        // Assert
        // 少し待って非同期の保存処理を待機
        await System.Threading.Tasks.Task.Delay(500);

        saveUseCaseMock.Verify(
            r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Tasks.Any(t => t.Name == taskName))),
            Times.AtLeastOnce(),
            "タスク追加時にリポジトリの SaveAsync が呼び出されること"
        );
    }

    /// <summary>
    /// テスト観点: 既存のタスクのプロパティを変更した際に、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task UpdateTaskProperty_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var task = new ProjectTask();
        task.UpdateName("Existing Task");
        task.AssignTo("Old User");
        var project = new Project { Id = projectId };
        project.UpdateName("SaveTest");
        project.AddTask(task);

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

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
        viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock
            .Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var saveCoordinator = new ProjectSaveCoordinator(
            saveUseCaseMock.Object,
            new Mock<ILogger<ProjectSaveCoordinator>>().Object
        );
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        var mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            projectServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            notificationServiceMock.Object,
            snackbarServiceMock.Object,
            osNotificationServiceMock.Object,
            checkDeadlinesUseCaseMock.Object,
            _dialogServiceMock.Object,
            _identityServiceMock.Object,
            _mainLoggerMock.Object
        );
        await System.Threading.Tasks.Task.Delay(100);

        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);
        var taskViewModel = projectViewModel.Tasks.First(t => t.Id == task.Id);

        // Act
        taskViewModel.Assignee = "New User";

        // Assert
        // 自動保存 (UpdatedAt変更トリガー) が完了するのを待つ
        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(
            r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Tasks.Any(t => t.Assignee == "New User"))),
            Times.AtLeastOnce(),
            "タスクのプロパティ変更時にリポジトリの SaveAsync が呼び出されること"
        );
    }

    /// <summary>
    /// テスト観点: プロジェクトのプロパティを変更した際に、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task UpdateProjectProperty_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("Old Name");

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

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

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock
            .Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var saveCoordinator = new ProjectSaveCoordinator(
            saveUseCaseMock.Object,
            new Mock<ILogger<ProjectSaveCoordinator>>().Object
        );
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        var mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            projectServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            notificationServiceMock.Object,
            snackbarServiceMock.Object,
            osNotificationServiceMock.Object,
            checkDeadlinesUseCaseMock.Object,
            _dialogServiceMock.Object,
            _identityServiceMock.Object,
            _mainLoggerMock.Object
        );
        await System.Threading.Tasks.Task.Delay(100);

        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);

        // Act
        projectViewModel.Name = "New Name";

        // Assert
        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(
            r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Name == "New Name")),
            Times.AtLeastOnce(),
            "プロジェクトのプロパティ変更時にリポジトリの SaveAsync が呼び出されること"
        );
    }
}
