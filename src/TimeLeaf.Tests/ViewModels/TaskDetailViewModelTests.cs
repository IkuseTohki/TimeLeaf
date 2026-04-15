using System;
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
public class TaskDetailViewModelTests
{
    private Mock<IAddCommentUseCase> _addCommentUseCaseMock = null!;
    private Mock<ILogger<TaskDetailViewModel>> _loggerMock = null!;
    private ProjectViewModel _projectViewModel = null!;
    private ProjectTaskViewModel _taskViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        _addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        _loggerMock = new Mock<ILogger<TaskDetailViewModel>>();

        var project = new Project();
        project.UpdateName("TestProject");
        var task = new ProjectTask();
        task.UpdateName("TestTask");

        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var userServiceMock = new Mock<IUserService>();

        _projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            viewModelFactoryMock.Object
        );
        _taskViewModel = new ProjectTaskViewModel(task, userServiceMock.Object);
    }

    /// <summary>
    /// テスト観点: ViewModel のプロパティ（実績開始日、実績終了日、実工数）を更新した際、
    /// 基になるモデル（ProjectTask）に正しく値が反映されることを確認する。
    /// </summary>
    [TestMethod]
    public void TaskProperties_WhenUpdated_ShouldReflectInModel()
    {
        // Arrange
        var vm = new TaskDetailViewModel(
            _projectViewModel,
            _taskViewModel,
            _addCommentUseCaseMock.Object,
            _loggerMock.Object
        );

        var expectedStart = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var expectedEnd = new DateTime(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var cost = 5.5;

        // Act
        vm.Task.ActualStartDate = expectedStart;
        vm.Task.ActualEndDate = expectedEnd;
        vm.Task.ActualCost = cost;

        // Assert
        Assert.IsNotNull(_taskViewModel.Model.ActualStartDate);
        Assert.IsNotNull(_taskViewModel.Model.ActualEndDate);
        Assert.AreEqual(expectedStart.Ticks, _taskViewModel.Model.ActualStartDate.Value.Ticks);
        Assert.AreEqual(expectedEnd.Ticks, _taskViewModel.Model.ActualEndDate.Value.Ticks);
        Assert.AreEqual(cost, _taskViewModel.Model.ActualCost);
    }

    [TestMethod]
    public void CloseCommand_WithBoolTrue_TriggersRequestCloseWithTrue()
    {
        // Arrange
        var vm = new TaskDetailViewModel(
            _projectViewModel,
            _taskViewModel,
            _addCommentUseCaseMock.Object,
            _loggerMock.Object
        );
        bool? resultReceived = null;
        vm.RequestClose += (res) => resultReceived = res;

        // Act
        vm.CloseCommand.Execute(true);

        // Assert
        Assert.IsTrue(resultReceived.HasValue);
        Assert.IsTrue(resultReceived.Value);
    }
}

[TestClass]
public class TaskSummaryViewModelTests
{
    private ProjectViewModel _projectViewModel = null!;
    private ProjectTaskViewModel _taskViewModel = null!;

    [TestInitialize]
    public void Initialize()
    {
        var project = new Project();
        project.UpdateName("TestProject");
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var userServiceMock = new Mock<IUserService>();

        _projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            viewModelFactoryMock.Object
        );
        var task = new ProjectTask();
        task.UpdateName("TestTask");
        _taskViewModel = new ProjectTaskViewModel(task, userServiceMock.Object);
    }

    [TestMethod]
    public void CloseCommand_WithStringTrue_TriggersRequestCloseWithTrue()
    {
        // Arrange
        var vm = new TaskSummaryViewModel(_projectViewModel, _taskViewModel);
        bool? resultReceived = null;
        vm.RequestClose += (res) => resultReceived = res;

        // Act
        vm.CloseCommand.Execute("True");

        // Assert
        Assert.IsTrue(resultReceived.HasValue);
        Assert.IsTrue(resultReceived.Value);
    }
}
