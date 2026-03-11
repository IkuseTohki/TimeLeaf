using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelTrayTests
{
    private Mock<ILoadProjectsUseCase> _loadUseCaseMock = null!;
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
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
        _loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
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

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            _loadUseCaseMock.Object,
            _saveUseCaseMock.Object,
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
    /// テスト観点: トレイからRequestOpenイベントが発生した際、IsWindowVisibleプロパティがtrueになることを確認する。
    /// </summary>
    [TestMethod]
    public void RequestOpen_ShouldSetIsWindowVisibleToTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.IsWindowVisible = false;

        // Act
        _osNotificationServiceMock.Raise(x => x.RequestOpen += null, EventArgs.Empty);

        // Assert
        Assert.IsTrue(viewModel.IsWindowVisible);
    }

    /// <summary>
    /// テスト観点: トレイからRequestExitイベントが発生した際、終了要求フラグ（CanExit）がtrueになることを確認する。
    /// </summary>
    [TestMethod]
    public void RequestExit_ShouldAllowApplicationExit()
    {
        // Arrange
        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.CanExit, "初期状態では終了不可（隠すだけ）であること");

        // Act
        _osNotificationServiceMock.Raise(x => x.RequestExit += null, EventArgs.Empty);

        // Assert
        Assert.IsTrue(viewModel.CanExit);
    }
}
