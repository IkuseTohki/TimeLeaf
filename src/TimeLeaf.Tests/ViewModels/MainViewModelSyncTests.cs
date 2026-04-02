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
public class MainViewModelSyncTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    /// <summary>
    /// テスト観点: 外部からの変更通知(ProjectChanged)による再ロード時に、
    /// 再度保存(SaveAsync)が走り、データが壊れないことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SyncReload_ShouldNotTriggerRedundantSave()
    {
        // Arrange
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var projectServiceMock = new Mock<IProjectService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();
        var dialogServiceMock = new Mock<IDialogService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _userServiceMock = new Mock<IUserService>();

        // ProjectWorkspaceViewModel 用のロガーもモックする
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(addProjectUseCaseMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);

        // Factory mock setup
        viewModelFactoryMock.Setup(x => x.CreateProjectViewModel(It.IsAny<Project>()))
            .Returns((Project p) => new ProjectViewModel(p, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, viewModelFactoryMock.Object));

        viewModelFactoryMock.Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));

        var projectId = Guid.NewGuid();
        var initialProject = new Project { Id = projectId };
        initialProject.UpdateName("Initial");

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { initialProject });

        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var notificationServiceMock = new Mock<INotificationService>();
        notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        var snackbarServiceMock = new Mock<ISnackbarService>();
        var osNotificationServiceMock = new Mock<IOSNotificationService>();
        var checkDeadlinesUseCaseMock = new Mock<ICheckTaskDeadlinesUseCase>();
        var identityServiceMock = new Mock<IIdentityService>();

        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var viewModel = new MainViewModel(loadUseCaseMock.Object, saveUseCaseMock.Object, findProjectUseCaseMock.Object, projectServiceMock.Object, addProjectUseCaseMock.Object, saveCoordinator, dispatcherMock.Object, viewModelFactoryMock.Object, notificationServiceMock.Object, snackbarServiceMock.Object, osNotificationServiceMock.Object, checkDeadlinesUseCaseMock.Object, dialogServiceMock.Object, identityServiceMock.Object, loggerMock.Object);
        await System.Threading.Tasks.Task.Delay(100); // InitializeAsync の完了を待つ

        // ロードされる「最新」の状態を準備（別のタスクがある状態）
        var updatedProject = new Project { Id = projectId };
        updatedProject.UpdateName("Updated");
        var taskFromSync = new ProjectTask();
        taskFromSync.UpdateName("Task from Sync");
        updatedProject.AddTask(taskFromSync); // 同期で追加されるタスク
        findProjectUseCaseMock.Setup(r => r.ExecuteAsync(projectId)).ReturnsAsync(updatedProject);

        // Act
        projectServiceMock.Raise(r => r.ProjectUpdated += null, updatedProject);
        await System.Threading.Tasks.Task.Delay(500); // OnProjectChanged 内の Dispatcher.InvokeAsync の完了をより長く待つ

        // Assert
        var project = viewModel.Projects.First(pvm => pvm.Id == projectId); // ViewModel 繧呈､懃ｴ｢

        Assert.AreEqual(1, project.Model.Tasks.Count, "同期によってタスクが1件に更新されていること");
        Assert.AreEqual("Task from Sync", project.Model.Tasks.First().Name, "同期された最新のタスク名が反映されていること");
    }
}
