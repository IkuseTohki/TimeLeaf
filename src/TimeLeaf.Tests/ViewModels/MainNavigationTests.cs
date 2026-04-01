using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainNavigationTests
{
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
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
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
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _userServiceMock = new Mock<IUserService>();

        _dispatcherServiceMock.Setup(x => x.InvokeAsync(It.IsAny<Action>()))
            .Callback<Action>(a => a())
            .Returns(Task.CompletedTask);

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());

        _viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object));

        _viewModelFactoryMock.Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));
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
    /// テスト観点: 初期状態のナビゲーションコンテキストが Home であり、CurrentViewModel が HomeViewModel であることを確認する。
    /// </summary>
    [TestMethod]
    public void DefaultNavigationContext_ShouldBeHome_AndSetHomeViewModel()
    {
        // Arrange
        var userMenu = new UserMenuViewModel(_identityServiceMock.Object, _dialogServiceMock.Object, _viewModelFactoryMock.Object);
        var expectedHome = new HomeViewModel(new ObservableCollection<ProjectViewModel>(), userMenu);
        _viewModelFactoryMock.Setup(x => x.CreateHomeViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>())).Returns(expectedHome);

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.AreEqual(MainNavigationContext.Home, viewModel.NavigationContext);
        Assert.AreSame(expectedHome, viewModel.CurrentViewModel);
    }

    /// <summary>
    /// テスト観点: ナビゲーションコンテキストを Notifications に切り替えた際、CurrentViewModel が NotificationsViewModel になることを確認する。
    /// </summary>
    [TestMethod]
    public void SwitchToNotifications_ShouldSetNotificationsViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var expectedNotifications = new NotificationsViewModel(_notificationServiceMock.Object, viewModel.Projects, new Mock<ILogger<NotificationsViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateNotificationsViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>(), null)).Returns(expectedNotifications);

        // Act
        viewModel.NavigationContext = MainNavigationContext.Notifications;

        // Assert
        Assert.AreEqual(MainNavigationContext.Notifications, viewModel.NavigationContext);
        Assert.AreSame(expectedNotifications, viewModel.CurrentViewModel);
    }

    /// <summary>
    /// テスト観点: サイドバーの初期状態が展開（True）であることを確認する。
    /// </summary>
    [TestMethod]
    public void Sidebar_InitialState_ShouldBeExpanded()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.IsSidebarExpanded);
    }

    /// <summary>
    /// テスト観点: サイドバーの開閉をトグルできることを確認する。
    /// </summary>
    [TestMethod]
    public void ToggleSidebar_ShouldChangeState()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act
        viewModel.ToggleSidebarCommand.Execute(null);

        // Assert
        Assert.IsFalse(viewModel.IsSidebarExpanded);

        // Act again
        viewModel.ToggleSidebarCommand.Execute(null);

        // Assert again
        Assert.IsTrue(viewModel.IsSidebarExpanded);
    }
}
