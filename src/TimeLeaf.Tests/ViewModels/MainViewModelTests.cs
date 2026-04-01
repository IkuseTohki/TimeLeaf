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
public class MainViewModelTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<ILoadProjectsUseCase> _loadUseCaseMock = null!;
    private Mock<ISaveProjectUseCase> _saveSingleUseCaseMock = null!;
    private Mock<IFindProjectUseCase> _findProjectUseCaseMock = null!;
    private Mock<IProjectSyncService> _syncServiceMock = null!;
    private Mock<IAddProjectUseCase> _addProjectUseCaseMock = null!;
    private Mock<IProjectSaveCoordinator> _saveCoordinatorMock = null!;
    private Mock<IDispatcherService> _dispatcherServiceMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<ISnackbarService> _snackbarServiceMock = null!;
    private Mock<IOSNotificationService> _osNotificationServiceMock = null!;
    private Mock<ICheckTaskDeadlinesUseCase> _checkDeadlinesUseCaseMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ICheckAssignmentUseCase> _checkAssignmentMock = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _userServiceMock = new Mock<IUserService>();
        _loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        _saveSingleUseCaseMock = new Mock<ISaveProjectUseCase>();
        _findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        _syncServiceMock = new Mock<IProjectSyncService>();
        _addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        _saveCoordinatorMock = new Mock<IProjectSaveCoordinator>();
        _dispatcherServiceMock = new Mock<IDispatcherService>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _snackbarServiceMock = new Mock<ISnackbarService>();
        _osNotificationServiceMock = new Mock<IOSNotificationService>();
        _checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();
        _dialogServiceMock = new Mock<IDialogService>();
        _identityServiceMock = new Mock<IIdentityService>();
        _checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        _loggerMock = new Mock<ILogger<MainViewModel>>();

        _dispatcherServiceMock.Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        _dispatcherServiceMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(f => f());

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());

        _viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object));

        _viewModelFactoryMock.Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(It.IsAny<ProjectViewModel>(), It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ProjectViewModel pvm, ObservableCollection<ProjectViewModel> projects) => new ProjectWorkspaceViewModel(pvm, projects, _notificationServiceMock.Object, _viewModelFactoryMock.Object, _checkAssignmentMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object));
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            _loadUseCaseMock.Object,
            _saveSingleUseCaseMock.Object,
            _findProjectUseCaseMock.Object,
            _syncServiceMock.Object,
            _addProjectUseCaseMock.Object,
            _saveCoordinatorMock.Object,
            _dispatcherServiceMock.Object,
            _viewModelFactoryMock.Object,
            _notificationServiceMock.Object,
            _snackbarServiceMock.Object,
            _osNotificationServiceMock.Object,
            _checkDeadlinesUseCaseMock.Object,
            _dialogServiceMock.Object,
            _identityServiceMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// テスト観点: 初期化完了後にタスクの期限チェックが実行されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Initialize_ShouldCheckDeadlines()
    {
        // Arrange
        var projects = new List<Project> { new Project() };
        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(projects);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(200); // Wait for InitializeAsync

        // Assert
        _checkDeadlinesUseCaseMock.Verify(x => x.Execute(It.IsAny<IEnumerable<Project>>()), Times.AtLeastOnce);
    }

    /// <summary>
    /// テスト観点: 通知サービスで未読数が増えた際、MainViewModel の UnreadNotificationCount が更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void UnreadNotificationCount_ShouldSyncWithNotificationService()
    {
        // Arrange
        var notifications = new List<Notification> { new Notification("Title", "Message") };
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(notifications);

        var viewModel = CreateViewModel();

        // Act
        // 擬似的に通知イベントを発生させる
        _notificationServiceMock.Raise(x => x.UnreadCountChanged += null, EventArgs.Empty);

        // Assert
        Assert.AreEqual(1, viewModel.UnreadNotificationCount);
    }

    /// <summary>
    /// テスト観点: MainViewModel の初期化時に、全プロジェクトがロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Initialize_ShouldLoadAllProjects()
    {
        // Arrange
        var projects = new List<Project> { new Project(), new Project() };
        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(projects);

        // Act
        var viewModel = CreateViewModel();
        await Task.Delay(100); // Wait for InitializeAsync

        // Assert
        Assert.AreEqual(2, viewModel.Projects.Count);
        _loadUseCaseMock.Verify(x => x.ExecuteAsync(), Times.Once);
    }

    /// <summary>
    /// テスト観点: プロジェクトを選択した際、画面が ProjectWorkspaceViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);

        var expectedWorkspace = new ProjectWorkspaceViewModel(projectViewModel, viewModel.Projects, _notificationServiceMock.Object, _viewModelFactoryMock.Object, _checkAssignmentMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(projectViewModel, viewModel.Projects)).Returns(expectedWorkspace);

        // Act
        viewModel.NavigateToProjectCommand.Execute(projectViewModel);

        // Assert
        Assert.AreSame(expectedWorkspace, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateProjectWorkspaceViewModel(projectViewModel, viewModel.Projects), Times.Once);
    }

    /// <summary>
    /// テスト観点: 戻るコマンドを実行した際、画面が HomeViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateBack_ShouldSetHomeViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // 一旦別のコンテキストにする（NavigateBack で Home に切り替わることを確認するため）
        _viewModelFactoryMock.Setup(x => x.CreateNotificationsViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>(), null))
            .Returns(new NotificationsViewModel(_notificationServiceMock.Object, viewModel.Projects, new Mock<ILogger<NotificationsViewModel>>().Object));

        viewModel.NavigationContext = MainNavigationContext.Notifications;

        var userMenu = new UserMenuViewModel(_identityServiceMock.Object, _dialogServiceMock.Object, _viewModelFactoryMock.Object);
        var expectedHome = new HomeViewModel(viewModel.Projects, userMenu);
        _viewModelFactoryMock.Setup(x => x.CreateHomeViewModel(viewModel.Projects)).Returns(expectedHome);

        // Act
        viewModel.NavigateBackCommand.Execute(null);

        // Assert
        Assert.AreSame(expectedHome, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateHomeViewModel(viewModel.Projects), Times.AtLeastOnce);
    }

    /// <summary>
    /// テスト観点: 全タスク一覧からタスク遷移が要求された際、該当プロジェクトのワークスペースへ遷移し、タスク詳細が開かれることを確認する。
    /// </summary>
    [TestMethod]
    public void AllTasks_RequestNavigation_ShouldNavigateToProjectAndTask()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);
        viewModel.Projects.Add(projectViewModel);

        var task = new ProjectTask();
        task.UpdateName("Target Task");
        var taskViewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);
        projectViewModel.Tasks.Add(taskViewModel);

        // AllTasksViewModel の実体作成（イベントを飛ばすため）
        var allTasksVm = new AllTasksViewModel(viewModel.Projects);
        _viewModelFactoryMock.Setup(x => x.CreateAllTasksViewModel(viewModel.Projects)).Returns(allTasksVm);

        // WorkspaceViewModel の実体作成
        var workspaceVm = new ProjectWorkspaceViewModel(
            projectViewModel,
            viewModel.Projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);

        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(projectViewModel, viewModel.Projects))
            .Returns(workspaceVm);

        // TaskDetailViewModel のモック作成
        var detailVm = new Mock<TaskDetailViewModel>(
            projectViewModel,
            taskViewModel,
            new Mock<IAddCommentUseCase>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object).Object;
        _viewModelFactoryMock.Setup(x => x.CreateTaskDetailViewModel(projectViewModel, taskViewModel))
            .Returns(detailVm);

        // Act
        // 1. AllTasks 画面へ遷移（これにより ViewModel が生成されイベントが購読される）
        viewModel.NavigationContext = MainNavigationContext.AllTasks;

        // 2. AllTasksViewModel からナビゲーションイベントを発火
        // task.ProjectName が一致する必要があるため、ViewModel 側のロジックに合わせる
        taskViewModel.ProjectName = "Test Project";
        allTasksVm.SelectTaskCommand.Execute(taskViewModel);

        // Assert
        Assert.AreEqual(MainNavigationContext.ProjectDetail, viewModel.NavigationContext);
        Assert.AreSame(workspaceVm, viewModel.CurrentViewModel);

        // WorkspaceViewModel の現在のサブビューがタスク詳細になっていることを確認
        Assert.AreSame(detailVm, workspaceVm.CurrentSubViewModel);
    }
}
