using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();

        var project = new Project();
        project.UpdateName("Test Project");
        _projectViewModel = new ProjectViewModel(project);

        // デフォルトの戻り値を設定
        _viewModelFactoryMock.Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel p) => new ProjectDashboardViewModel(p, new Mock<IAddMilestoneUseCase>().Object, new Mock<ILogger<ProjectDashboardViewModel>>().Object));

        var detailLoggerMock = new Mock<ILogger<TaskDetailViewModel>>();
        _viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel p) => new ProjectTasksViewModel(
                p,
                new Mock<IAddTaskUseCase>().Object,
                _viewModelFactoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<ILogger<ProjectTasksViewModel>>().Object,
                detailLoggerMock.Object));
    }

    [TestMethod]
    public void DefaultView_ShouldBeDashboard()
    {
        // Act
        var vm = new ProjectWorkspaceViewModel(_projectViewModel, _notificationServiceMock.Object, _viewModelFactoryMock.Object, _loggerMock.Object);

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectDashboardViewModel));
    }

    [TestMethod]
    public void SwitchToTasks_ShouldUpdateCurrentSubViewModel()
    {
        // Arrange
        var vm = new ProjectWorkspaceViewModel(_projectViewModel, _notificationServiceMock.Object, _viewModelFactoryMock.Object, _loggerMock.Object);

        // Act
        vm.SwitchSubViewCommand.Execute("Tasks");

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectTasksViewModel));
    }
}
