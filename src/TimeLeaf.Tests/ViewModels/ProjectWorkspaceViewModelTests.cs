using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LeafKit.UI.Services;
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
public class ProjectWorkspaceViewModelTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<ICheckAssignmentUseCase> _checkAssignmentMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private ProjectViewModel _projectViewModel = null!;
    private ObservableCollection<ProjectViewModel> _projects = null!;

    [TestInitialize]
    public void Initialize()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        _userServiceMock = new Mock<IUserService>();
        _projects = new ObservableCollection<ProjectViewModel>();

        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        _projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Factory mock setup
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

        // デフォルトの戻り値を設定
        _viewModelFactoryMock
            .Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(
                (ProjectViewModel p) =>
                    new ProjectDashboardViewModel(
                        p,
                        new Mock<IAddMilestoneUseCase>().Object,
                        new Mock<ILogger<ProjectDashboardViewModel>>().Object
                    )
            );

        var detailLoggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(
                (ProjectViewModel p) =>
                    new ProjectTasksViewModel(
                        p,
                        new Mock<IAddTaskUseCase>().Object,
                        new Mock<IGetProjectMembersUseCase>().Object,
                        _viewModelFactoryMock.Object,
                        new Mock<IDialogService>().Object,
                        new DetectProjectRisksUseCase(new CalculateCriticalPathUseCase()),
                        new CalculateFlowLayoutUseCase(),
                        new Mock<ILogger<ProjectTasksViewModel>>().Object,
                        new Mock<ILogger<TaskDetailViewModel>>().Object
                    )
            );
    }

    [TestMethod]
    public void IsUserAssigned_ShouldReflectProjectViewModelStatus()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var project = new Project(Guid.Empty);
        // 自分はまだアサインされていない
        var projectVm = new ProjectViewModel(
            project,
            myId,
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        var vm = new ProjectWorkspaceViewModel(
            projectVm,
            _projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            _loggerMock.Object
        );

        // Assert
        Assert.IsFalse(vm.IsUserAssigned);

        // Act: アサインする
        project.AssignUser(myId);
        projectVm.SyncFromModel();

        // Assert: 連動して true になる
        Assert.IsTrue(vm.IsUserAssigned);
    }

    [TestMethod]
    public void IsUserAssigned_ShouldNotifyChange_WhenProjectViewModelAssignmentChanges()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var project = new Project(Guid.Empty);
        var projectVm = new ProjectViewModel(
            project,
            myId,
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var vm = new ProjectWorkspaceViewModel(
            projectVm,
            _projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            _loggerMock.Object
        );

        bool notified = false;
        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectWorkspaceViewModel.IsUserAssigned))
            {
                notified = true;
            }
        };

        // Act
        project.AssignUser(myId);
        projectVm.SyncFromModel();

        // Assert
        Assert.IsTrue(notified);
    }

    [TestMethod]
    public void DefaultView_ShouldBeDashboard()
    {
        // Act
        var vm = new ProjectWorkspaceViewModel(
            _projectViewModel,
            _projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            _loggerMock.Object
        );

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectDashboardViewModel));
    }

    [TestMethod]
    public void SwitchToTasks_ShouldUpdateCurrentSubViewModel()
    {
        // Arrange
        var vm = new ProjectWorkspaceViewModel(
            _projectViewModel,
            _projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            _loggerMock.Object
        );

        // Act
        vm.SwitchSubViewCommand.Execute("Tasks");

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectTasksViewModel));
    }

    [TestMethod]
    public void OpenTaskDetail_ShouldSwitchSubViewToTaskDetail()
    {
        // Arrange
        var vm = new ProjectWorkspaceViewModel(
            _projectViewModel,
            _projects,
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            _checkAssignmentMock.Object,
            _loggerMock.Object
        );
        var task = new ProjectTask { Id = Guid.NewGuid() };
        var taskVm = new ProjectTaskViewModel(task, _userServiceMock.Object);
        var detailVm = new TaskDetailViewModel(
            _projectViewModel,
            taskVm,
            new Mock<IAddCommentUseCase>().Object,
            new Mock<ISaveProjectUseCase>().Object,
            new Mock<IDeleteTaskUseCase>().Object,
            new Mock<IDialogService>().Object,
            _userServiceMock.Object,
            new Mock<IProjectService>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object
        );

        _viewModelFactoryMock.Setup(x => x.CreateTaskDetailViewModel(_projectViewModel, taskVm)).Returns(detailVm);

        // Act
        vm.OpenTaskDetailCommand.Execute(taskVm);

        // Assert
        // オーバーレイ用プロパティではなく、メインコンテンツが切り替わることを確認
        Assert.AreEqual(detailVm, vm.CurrentSubViewModel);
    }
}
