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
public class EndToEndFlowTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<MainViewModel>> _mainLoggerMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project>());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
        _mainLoggerMock = new Mock<ILogger<MainViewModel>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _dialogServiceMock = new Mock<IDialogService>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(_userServiceMock.Object);
        var saveProjectUseCase = new SaveProjectUseCase(_repositoryMock.Object, _userServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(new AddProjectUseCase(saveProjectUseCase));
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(_dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// テスト観点: プロジェクト作成からタスク追加、プロパティ変更までの一連の操作が、
    /// 正しくリポジトリの保存処理(SaveAsync)に繋がることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task CreateProjectAndAddTask_ShouldFlowToRepository()
    {
        // 1. Initialize mocks and MainViewModel
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCase = new AddProjectUseCase(new SaveProjectUseCase(_repositoryMock.Object, _userServiceMock.Object));
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        var allProjects = new List<Project>();
        loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(allProjects);

        // AddProjectUseCase はリポジトリに保存し、かつプロジェクトのリストにも追加するようにコールバックを設定
        _repositoryMock.Setup(r => r.SaveAsync(It.IsAny<Project>(), It.IsAny<string>()))
            .Callback<Project, string>((p, u) => allProjects.Add(p))
            .Returns(Task.CompletedTask);

        var initialProjects = new ObservableCollection<ProjectViewModel>();
        var overviewVM = new OverviewViewModel(initialProjects, addProjectUseCase, _dialogServiceMock.Object, viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object);
        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns(overviewVM);

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();

        var loggerMock = new Mock<ILogger<MainViewModel>>();
        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);

        var mainVM = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            syncServiceMock.Object,
            addProjectUseCase,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            notificationServiceMock.Object,
            snackbarServiceMock.Object,
            osNotificationServiceMock.Object,
            checkDeadlinesUseCaseMock.Object,
            loggerMock.Object);

        await System.Threading.Tasks.Task.Delay(100);

        // 2. Add a new project (Overview -> Dialog -> UseCase -> MainVM.Projects)
        var addProjectVm = new AddProjectViewModel { Name = "E2E Project" };
        viewModelFactoryMock.Setup(x => x.CreateAddProjectViewModel()).Returns(addProjectVm);
        _dialogServiceMock.Setup(ds => ds.ShowDialogAsync(addProjectVm)).ReturnsAsync(true);

        await overviewVM.AddProjectCommand.ExecuteAsync(null);

        // プロジェクト追加後、リポジトリの変更イベントをシミュレートして MainViewModel に再ロードさせる
        var addedProject = allProjects.First();
        findProjectUseCaseMock.Setup(r => r.ExecuteAsync(addedProject.Id)).ReturnsAsync(addedProject);
        syncServiceMock.Raise(s => s.ProjectChanged += null, addedProject.Id);
        await Task.Delay(200);

        var projectVM = mainVM.Projects.First(p => p.Name == "E2E Project");
        // プロジェクト追加時に UseCase (AddProjectUseCase) が直接リポジトリを呼ぶため、ここでリポジトリを検証してOK
        _repositoryMock.Verify(r => r.SaveAsync(projectVM.Model, "test-user"), Times.AtLeastOnce(), "プロジェクト作成時に保存されること");

        // 3. Navigate to Project
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(saveUseCaseMock.Object, new Mock<ICurrentUserService>().Object);
        var addMilestoneUseCase = new AddMilestoneUseCase(saveUseCaseMock.Object);

        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, notificationServiceMock.Object, viewModelFactoryMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            addTaskUseCase,
            viewModelFactoryMock.Object,
            _dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(projectVM)).Returns(workspaceVM);
        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectVM)).Returns(tasksVM);

        mainVM.NavigateToProjectCommand.Execute(projectVM);
        workspaceVM.SwitchSubViewCommand.Execute("Tasks");

        // 4. Add a Task in Workspace
        var taskName = "E2E Task";
        var addTaskViewModel = new AddTaskViewModel { Name = taskName };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel()).Returns(addTaskViewModel);
        _dialogServiceMock.Setup(ds => ds.ShowDialogAsync(addTaskViewModel)).ReturnsAsync(true);

        await tasksVM.AddTaskCommand.ExecuteAsync(null);

        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectVM.Id && p.Tasks.Any(t => t.Name == taskName))), Times.AtLeastOnce(), "タスク追加時に保存されること");

        // 5. Update Task Property
        var taskVM = projectVM.Tasks.First(t => t.Name == "E2E Task");
        taskVM.Assignee = "E2E User";

        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectVM.Id && p.Tasks.Any(t => t.Assignee == "E2E User"))), Times.AtLeastOnce(), "タスクの属性変更時に保存されること");
    }
}
