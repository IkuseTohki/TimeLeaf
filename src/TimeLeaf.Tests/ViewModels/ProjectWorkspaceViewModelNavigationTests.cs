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
public class ProjectWorkspaceViewModelNavigationTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private Mock<IUserService> _userServiceMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _notificationServiceMock = new Mock<INotificationService>();
        _notificationServiceMock.Setup(x => x.UnreadNotifications).Returns(new List<Notification>());
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();

        var project = new Project();
        project.UpdateName("Nav Test Project");
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

        _viewModelFactoryMock
            .Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(
                (ProjectViewModel pvm) =>
                    new ProjectDashboardViewModel(
                        pvm,
                        new Mock<IAddMilestoneUseCase>().Object,
                        new Mock<ILogger<ProjectDashboardViewModel>>().Object
                    )
            );

        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns(
                (ProjectViewModel pvm) =>
                    new ProjectTasksViewModel(
                        pvm,
                        new Mock<IAddTaskUseCase>().Object,
                        new Mock<IGetProjectMembersUseCase>().Object,
                        _viewModelFactoryMock.Object,
                        new Mock<IDialogService>().Object,
                        new Mock<ILogger<ProjectTasksViewModel>>().Object,
                        new Mock<ILogger<TaskDetailViewModel>>().Object
                    )
            );
    }

    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var checkAssignmentMock = new Mock<ICheckAssignmentUseCase>();
        var vm = new ProjectWorkspaceViewModel(
            _projectViewModel,
            new ObservableCollection<ProjectViewModel>(),
            _notificationServiceMock.Object,
            _viewModelFactoryMock.Object,
            checkAssignmentMock.Object,
            _loggerMock.Object
        );

        // Act
        vm.SwitchSubViewCommand.Execute("Tasks");

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectTasksViewModel));
    }
}
