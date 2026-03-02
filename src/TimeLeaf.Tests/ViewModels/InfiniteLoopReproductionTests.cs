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

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class InfiniteLoopReproductionTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project>());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(_userServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// バグ再現テスト: 特定条件下で PropertyChanged がループして保存が無限に走らないことを確認。
    /// （課題: ViewModel の計算プロパティ変更がさらにモデル変更を誘発して再保存されるループ）
    /// </summary>
    [TestMethod]
    public async Task AddingMultipleTasksWithCost_ShouldNotLoop()
    {
        // 1. Arrange
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var projectEntity = new Project();
        projectEntity.UpdateName("LoopTest");
        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { projectEntity });

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var mainVM = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            syncServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            _loggerMock.Object);
        await Task.Delay(100); // Wait for initialize

        var projectVM = mainVM.Projects.First();
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(saveUseCaseMock.Object, _userServiceMock.Object);
        var addMilestoneUseCase = new AddMilestoneUseCase(saveUseCaseMock.Object);

        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, viewModelFactoryMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        var dialogServiceMock = new Mock<LeafKit.UI.Services.IDialogService>();
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            addTaskUseCase,
            viewModelFactoryMock.Object,
            dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectVM)).Returns(tasksVM);
        workspaceVM.SwitchSubViewCommand.Execute("Tasks");

        // 2. Act - Add a task with estimated cost
        var addTaskViewModel1 = new AddTaskViewModel { Name = "Task 1" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel()).Returns(addTaskViewModel1);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel1)).ReturnsAsync(true);

        await tasksVM.AddTaskCommand.ExecuteAsync(null);
        var task1 = projectVM.Tasks.First();
        task1.EstimatedCost = 10.0;

        await Task.Delay(500); // Wait for potential async propagation

        // 3. Assert - 保存回数が異常に多くないこと（10回程度なら許容、無限なら数百回になる）
        var saveCount = saveUseCaseMock.Invocations.Count(i => i.Method.Name == "ExecuteAsync");
        Assert.IsTrue(saveCount < 10, $"Save count is too high ({saveCount}), possible loop detected.");
    }

    [TestMethod]
    public async Task RapidSuccessiveUpdates_ShouldNotCauseOverlappingSaves()
    {
        // 1. Arrange
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var dispatcherMock = new Mock<IDispatcherService>();
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Action>())).Callback<Action>(a => a()).Returns(Task.CompletedTask);
        dispatcherMock.Setup(x => x.InvokeAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(f => f());

        var projectEntity = new Project();
        projectEntity.UpdateName("ConcurrencyTest");
        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { projectEntity });

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, viewModelFactoryMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var saveCoordinator = new ProjectSaveCoordinator(saveUseCaseMock.Object, new Mock<ILogger<ProjectSaveCoordinator>>().Object);
        var mainVM = new MainViewModel(
            loadUseCaseMock.Object,
            saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            syncServiceMock.Object,
            addProjectUseCaseMock.Object,
            saveCoordinator,
            dispatcherMock.Object,
            viewModelFactoryMock.Object,
            _loggerMock.Object);
        await Task.Delay(100);

        var projectVM = mainVM.Projects.First();
        var addTaskUseCase = new AddTaskUseCase(saveUseCaseMock.Object);
        var addCommentUseCase = new AddCommentUseCase(saveUseCaseMock.Object, _userServiceMock.Object);
        var addMilestoneUseCase = new AddMilestoneUseCase(saveUseCaseMock.Object);

        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, viewModelFactoryMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        var dialogServiceMock = new Mock<LeafKit.UI.Services.IDialogService>();
        var tasksVM = new ProjectTasksViewModel(
            projectVM,
            addTaskUseCase,
            viewModelFactoryMock.Object,
            dialogServiceMock.Object,
            new Mock<ILogger<ProjectTasksViewModel>>().Object,
            new Mock<ILogger<TaskDetailViewModel>>().Object);

        viewModelFactoryMock.Setup(x => x.CreateProjectTasksViewModel(projectVM)).Returns(tasksVM);
        workspaceVM.SwitchSubViewCommand.Execute("Tasks");

        // 2. Act - Add 1st task
        var addTaskViewModel1 = new AddTaskViewModel { Name = "Task 1" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel()).Returns(addTaskViewModel1);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel1)).ReturnsAsync(true);
        await tasksVM.AddTaskCommand.ExecuteAsync(null);

        // 3. Act - Add 2nd task immediately
        var addTaskViewModel2 = new AddTaskViewModel { Name = "Task 2" };
        viewModelFactoryMock.Setup(x => x.CreateAddTaskViewModel()).Returns(addTaskViewModel2);
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addTaskViewModel2)).ReturnsAsync(true);
        await tasksVM.AddTaskCommand.ExecuteAsync(null);

        await Task.Delay(500);

        // 4. Assert - 両方の追加が反映され、保存が走っていること
        Assert.AreEqual(2, projectVM.Tasks.Count);
        var saveCount = saveUseCaseMock.Invocations.Count(i => i.Method.Name == "ExecuteAsync");
        Assert.IsTrue(saveCount < 10, $"Save count is too high ({saveCount}), possible loop detected.");
    }
}
