using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelNavigationTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();

        var project = new Project();
        project.UpdateName("Nav Test Project");
        _projectViewModel = new ProjectViewModel(project);

        // Factory mock setup
        _viewModelFactoryMock.Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectDashboardViewModel(pvm, new Mock<IAddMilestoneUseCase>().Object, new Mock<ILogger<ProjectDashboardViewModel>>().Object));

        _viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectTasksViewModel(
                pvm,
                new Mock<IAddTaskUseCase>().Object,
                _viewModelFactoryMock.Object,
                new Mock<IDialogService>().Object,
                new Mock<ILogger<ProjectTasksViewModel>>().Object,
                new Mock<ILogger<TaskDetailViewModel>>().Object));
    }

    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var vm = new ProjectWorkspaceViewModel(_projectViewModel, _viewModelFactoryMock.Object, _loggerMock.Object);

        // Act
        vm.SwitchSubViewCommand.Execute("Tasks");

        // Assert
        Assert.IsInstanceOfType(vm.CurrentSubViewModel, typeof(ProjectTasksViewModel));
    }
}
