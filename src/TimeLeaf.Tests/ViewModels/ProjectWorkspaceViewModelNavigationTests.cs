using System;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelNavigationTests
{
    private Mock<IAddTaskUseCase> _addTaskUseCaseMock = null!;
    private Mock<IAddCommentUseCase> _addCommentUseCaseMock = null!;
    private Mock<IAddMilestoneUseCase> _addMilestoneUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _addTaskUseCaseMock = new Mock<IAddTaskUseCase>();
        _addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        _addMilestoneUseCaseMock = new Mock<IAddMilestoneUseCase>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        _projectViewModel = new ProjectViewModel(new TimeLeaf.Models.Entities.Project());

        // Factory mock setups
        _viewModelFactoryMock.Setup(x => x.CreateProjectDashboardViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectDashboardViewModel(pvm, _addMilestoneUseCaseMock.Object, new Mock<ILogger<ProjectDashboardViewModel>>().Object));
        _viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectTasksViewModel(pvm, _addTaskUseCaseMock.Object, _addCommentUseCaseMock.Object, new Mock<ILogger<ProjectTasksViewModel>>().Object, new Mock<ILogger<TaskDetailViewModel>>().Object));
        _viewModelFactoryMock.Setup(x => x.CreateProjectTimelineViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectTimelineViewModel(pvm));
        _viewModelFactoryMock.Setup(x => x.CreateProjectSettingsViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectSettingsViewModel(pvm));
    }

    [TestMethod]
    public void DefaultView_ShouldBeDashboard()
    {
        // Arrange
        var viewModel = new ProjectWorkspaceViewModel(
            _projectViewModel,
            _viewModelFactoryMock.Object,
            _loggerMock.Object);

        // Act & Assert
        Assert.IsNotNull(viewModel.CurrentSubViewModel, "初期状態では Dashboard が設定されていること");
        Assert.IsInstanceOfType(viewModel.CurrentSubViewModel, typeof(ProjectDashboardViewModel));
    }

    [TestMethod]
    public void SwitchToTasks_ShouldUpdateCurrentSubViewModel()
    {
        // Arrange
        var viewModel = new ProjectWorkspaceViewModel(
            _projectViewModel,
            _viewModelFactoryMock.Object,
            _loggerMock.Object);

        // Act
        viewModel.SwitchSubViewCommand.Execute("Tasks");

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentSubViewModel, typeof(ProjectTasksViewModel));
    }
}
