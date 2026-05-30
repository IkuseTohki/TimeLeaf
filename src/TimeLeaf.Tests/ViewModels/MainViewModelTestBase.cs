using LeafKit.UI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using TimeLeaf.Models;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

public abstract class MainViewModelTestBase
{
    protected Mock<ILoadProjectsUseCase> LoadUseCaseMock = new();
    protected Mock<ISaveProjectUseCase> SaveUseCaseMock = new();
    protected Mock<IFindProjectUseCase> FindProjectUseCaseMock = new();
    protected Mock<IProjectService> ProjectServiceMock = new();
    protected Mock<IAddProjectUseCase> AddProjectUseCaseMock = new();
    protected Mock<IProjectSaveCoordinator> SaveCoordinatorMock = new();
    protected Mock<IDispatcherService> DispatcherMock = new();
    protected Mock<IViewModelFactory> ViewModelFactoryMock = new();
    protected Mock<INotificationService> NotificationServiceMock = new();
    protected Mock<ISnackbarService> SnackbarServiceMock = new();
    protected Mock<IOSNotificationService> OsNotificationServiceMock = new();
    protected Mock<ICheckTaskDeadlinesUseCase> CheckDeadlinesUseCaseMock = new();
    protected Mock<IDialogService> DialogServiceMock = new();
    protected Mock<IIdentityService> IdentityServiceMock = new();
    protected Mock<IUserService> UserServiceMock = new();
    protected Mock<IApplicationSettingsRepository> ApplicationSettingsRepositoryMock = new();
    protected ApplicationSettings ApplicationSettings = new();
    protected Mock<ILogger<MainViewModel>> LoggerMock = new();

    protected MainViewModel CreateMainViewModel()
    {
        return new MainViewModel(
            LoadUseCaseMock.Object,
            SaveUseCaseMock.Object,
            FindProjectUseCaseMock.Object,
            ProjectServiceMock.Object,
            AddProjectUseCaseMock.Object,
            SaveCoordinatorMock.Object,
            DispatcherMock.Object,
            ViewModelFactoryMock.Object,
            NotificationServiceMock.Object,
            SnackbarServiceMock.Object,
            OsNotificationServiceMock.Object,
            CheckDeadlinesUseCaseMock.Object,
            DialogServiceMock.Object,
            IdentityServiceMock.Object,
            UserServiceMock.Object,
            ApplicationSettingsRepositoryMock.Object,
            ApplicationSettings,
            LoggerMock.Object
        );
    }
}
