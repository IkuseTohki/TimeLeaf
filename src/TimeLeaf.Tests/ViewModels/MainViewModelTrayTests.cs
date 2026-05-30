using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using LeafKit.UI.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelTrayTests
{
    private Mock<ILoadProjectsUseCase> _loadUseCaseMock = null!;
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<IFindProjectUseCase> _findProjectUseCaseMock = null!;
    private Mock<IProjectService> _projectServiceMock = null!;
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
    private Mock<IApplicationSettingsRepository> _settingsRepoMock = null!;
    private ApplicationSettings _settings = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        _projectServiceMock = new Mock<IProjectService>();
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
        _settingsRepoMock = new Mock<IApplicationSettingsRepository>();
        _settings = new ApplicationSettings();
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _userServiceMock = new Mock<IUserService>();

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());

        _viewModelFactoryMock
            .Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns(
                (Project p) =>
                    new ProjectViewModel(
                        p,
                        Guid.NewGuid(),
                        new Mock<IJoinProjectUseCase>().Object,
                        _viewModelFactoryMock.Object
                    )
            );

        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            _loadUseCaseMock.Object,
            _saveUseCaseMock.Object,
            _findProjectUseCaseMock.Object,
            _projectServiceMock.Object,
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
            _userServiceMock.Object,
            _settingsRepoMock.Object,
            _settings,
            _loggerMock.Object
        );
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
        _settings.MinimizeOnClose = true;
        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.CanExit, "初期状態（MinimizeOnClose=true）では終了不可であること");

        // Act
        _osNotificationServiceMock.Raise(x => x.RequestExit += null, EventArgs.Empty);

        // Assert
        Assert.IsTrue(viewModel.CanExit, "トレイからの終了要求時は設定に関わらず終了可能になること");
    }

    /// <summary>
    /// テスト観点: MinimizeOnClose設定がfalseの場合、CanExitが最初からtrueになることを確認する。
    /// </summary>
    [TestMethod]
    public void CanExit_ShouldBeTrue_WhenMinimizeOnCloseIsFalse()
    {
        // Arrange
        _settings.MinimizeOnClose = false;

        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsTrue(viewModel.CanExit, "MinimizeOnCloseがfalseなら(X)で終了できるべき");
    }

    /// <summary>
    /// テスト観点: 実行中に設定が変更された際、CanExitプロパティが追従して更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void CanExit_ShouldUpdate_WhenSettingsChanged()
    {
        // Arrange
        _settings.MinimizeOnClose = true;
        var viewModel = CreateViewModel();
        Assert.IsFalse(viewModel.CanExit);

        // Act
        _settings.MinimizeOnClose = false;
        _settingsRepoMock.Raise(x => x.SettingsChanged += null, null, _settings);

        // Assert
        Assert.IsTrue(viewModel.CanExit, "設定変更後にCanExitが更新されること");
    }
}
