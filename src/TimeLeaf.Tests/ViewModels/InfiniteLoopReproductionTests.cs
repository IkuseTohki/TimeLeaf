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
public class InfiniteLoopReproductionTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project>());
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _userServiceMock = new Mock<IUserService>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IIdentityService)))
            .Returns(_identityServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// バグ再現テスト: 特定条件下で PropertyChanged がループして保存が無限に走らないことを確認。
    /// </summary>
    [TestMethod]
    public async Task AddingMultipleTasksWithCost_ShouldNotLoop()
    {
        // 1. Arrange
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, _testUserId, new Mock<IJoinProjectUseCase>().Object, viewModelFactoryMock.Object));
        viewModelFactoryMock.Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var projectEntity = new Project();
        projectEntity.UpdateName("LoopTest");
        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { projectEntity });

        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        var mainVM = new MainViewModel(
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
            new Mock<IDialogService>().Object,
            _identityServiceMock.Object,
            _loggerMock.Object);

        await Task.Delay(100); // Wait for initialize

        var projectVM = mainVM.Projects.First();
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);

        var checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, mainVM.Projects, notificationServiceMock.Object, viewModelFactoryMock.Object, checkAssignmentMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        var dialogServiceMock = new Mock<IDialogService>();
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            addTaskUseCase,
            new Mock<IUserRepository>().Object,
            viewModelFactoryMock.Object,
            dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectVM)).Returns(tasksVM);
        workspaceVM.SwitchSubViewCommand.Execute("Tasks");

        // 2. Act - Add a task with estimated cost
        var addTaskViewModel1 = new AddTaskViewModel { Name = "Task 1" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel(It.IsAny<IEnumerable<User>>())).Returns(addTaskViewModel1);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel1)).ReturnsAsync(true);

        await tasksVM.AddTaskCommand.ExecuteAsync(null);
        var task1 = projectVM.Tasks.First();
        task1.EstimatedCost = 10.0;

        await Task.Delay(500); // Wait for potential async propagation

        // 3. Assert
        var saveCount = saveUseCaseMock.Invocations.Count(i => i.Method.Name == "ExecuteAsync");
        Assert.IsTrue(saveCount < 10, $"Save count is too high ({saveCount}), possible loop detected.");
    }

    [TestMethod]
    public async Task RapidSuccessiveUpdates_ShouldNotCauseOverlappingSaves()
    {
        // 1. Arrange
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, _testUserId, new Mock<IJoinProjectUseCase>().Object, viewModelFactoryMock.Object));
        viewModelFactoryMock.Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var projectEntity = new Project();
        projectEntity.UpdateName("ConcurrencyTest");
        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { projectEntity });

        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        var mainVM = new MainViewModel(
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
            new Mock<IDialogService>().Object,
            _identityServiceMock.Object,
            _loggerMock.Object);

        await Task.Delay(100);

        var projectVM = mainVM.Projects.First();
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);

        var checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, mainVM.Projects, notificationServiceMock.Object, viewModelFactoryMock.Object, checkAssignmentMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        var dialogServiceMock = new Mock<IDialogService>();
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            addTaskUseCase,
            new Mock<IUserRepository>().Object,
            viewModelFactoryMock.Object,
            dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectVM)).Returns(tasksVM);
        workspaceVM.SwitchSubViewCommand.Execute("Tasks");

        // 2. Act - Add 1st task
        var addTaskViewModel1 = new AddTaskViewModel { Name = "Task 1" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel(It.IsAny<IEnumerable<User>>())).Returns(addTaskViewModel1);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel1)).ReturnsAsync(true);
        await tasksVM.AddTaskCommand.ExecuteAsync(null);

        // 3. Act - Add 2nd task immediately
        var addTaskViewModel2 = new AddTaskViewModel { Name = "Task 2" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel(It.IsAny<IEnumerable<User>>())).Returns(addTaskViewModel2);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel2)).ReturnsAsync(true);
        await tasksVM.AddTaskCommand.ExecuteAsync(null);

        await Task.Delay(500);

        // 4. Assert
        Assert.AreEqual(2, projectVM.Tasks.Count);
        var saveCount = saveUseCaseMock.Invocations.Count(i => i.Method.Name == "ExecuteAsync");
        Assert.IsTrue(saveCount < 10, $"Save count is too high ({saveCount}), possible loop detected.");
    }
}
