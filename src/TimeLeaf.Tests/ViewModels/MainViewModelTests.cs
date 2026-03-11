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
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
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
        _loggerMock = new Mock<ILogger<MainViewModel>>();

        _dispatcherServiceMock.Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);
        _dispatcherServiceMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(f => f());

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());

        _viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, _addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, _viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectWorkspaceViewModel(pvm, _notificationServiceMock.Object, _viewModelFactoryMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object));
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
        var projectViewModel = new ProjectViewModel(project);

        var expectedWorkspace = new ProjectWorkspaceViewModel(projectViewModel, _notificationServiceMock.Object, _viewModelFactoryMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(projectViewModel)).Returns(expectedWorkspace);

        // Act
        viewModel.NavigateToProjectCommand.Execute(projectViewModel);

        // Assert
        Assert.AreSame(expectedWorkspace, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateProjectWorkspaceViewModel(projectViewModel), Times.Once);
    }

    /// <summary>
    /// テスト観点: 戻るコマンドを実行した際、画面が OverviewViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateBack_ShouldSetOverviewViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();

        var expectedOverview = new OverviewViewModel(viewModel.Projects, _addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, _viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(viewModel.Projects)).Returns(expectedOverview);

        // Act
        viewModel.NavigateBackCommand.Execute(null);

        // Assert
        Assert.AreSame(expectedOverview, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateOverviewViewModel(viewModel.Projects), Times.AtLeastOnce);
    }
}
