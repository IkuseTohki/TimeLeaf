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

using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectWorkspaceViewModelSaveTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ILogger<MainViewModel>> _mainLoggerMock = null!;
    private Mock<ILogger<OverviewViewModel>> _overviewLoggerMock = null!;
    private Mock<ILogger<ProjectWorkspaceViewModel>> _workspaceLoggerMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _mainLoggerMock = new Mock<ILogger<MainViewModel>>();
        _overviewLoggerMock = new Mock<ILogger<OverviewViewModel>>();
        _workspaceLoggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _dialogServiceMock = new Mock<IDialogService>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(_workspaceLoggerMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(_overviewLoggerMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(_userServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(new Mock<IAddProjectUseCase>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(_dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// テスト観点: WorkspaceViewModel でタスクを追加した際に、MainViewModel の自動保存ロジックが走り、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddTaskInWorkspace_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("SaveTest");

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, _serviceProviderMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var mainViewModel = new MainViewModel(loadUseCaseMock.Object, saveUseCaseMock.Object, findProjectUseCaseMock.Object, syncServiceMock.Object, addProjectUseCaseMock.Object, viewModelFactoryMock.Object, _mainLoggerMock.Object);
        await System.Threading.Tasks.Task.Delay(100); // InitializeAsync の完了を待つ

        // MainViewModel.Projects から該当の ViewModel を取得
        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);

        // MainViewModel から WorkspaceViewModel へ遷移したと仮定
        var workspaceViewModel = new ProjectWorkspaceViewModel(projectViewModel, _userServiceMock.Object, _workspaceLoggerMock.Object);

        // Act
        workspaceViewModel.NewTaskName = "New Task to Save";
        workspaceViewModel.AddTaskCommand.Execute(null);

        // Assert
        // 少し待って非同期の保存処理を待機
        await System.Threading.Tasks.Task.Delay(500);

        saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Tasks.Any(t => t.Name == "New Task to Save"))), Times.AtLeastOnce(), "タスク追加時にリポジトリの SaveAsync が呼び出されること");
    }

    /// <summary>
    /// テスト観点: 既存のタスクのプロパティを変更した際に、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task UpdateTaskProperty_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var task = new ProjectTask { Name = "Existing Task", Assignee = "Old User" };
        var project = new Project { Id = projectId };
        project.UpdateName("SaveTest");
        project.AddTask(task);

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, _serviceProviderMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var mainViewModel = new MainViewModel(loadUseCaseMock.Object, saveUseCaseMock.Object, findProjectUseCaseMock.Object, syncServiceMock.Object, addProjectUseCaseMock.Object, viewModelFactoryMock.Object, _mainLoggerMock.Object);
        await System.Threading.Tasks.Task.Delay(100);

        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);
        var taskViewModel = projectViewModel.Tasks.First(t => t.Id == task.Id);

        // Act
        taskViewModel.Assignee = "New User";

        // Assert
        // 自動保存 (UpdatedAt変更トリガー) が完了するのを待つ
        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Tasks.Any(t => t.Assignee == "New User"))), Times.AtLeastOnce(), "タスクのプロパティ変更時にリポジトリの SaveAsync が呼び出されること");
    }

    /// <summary>
    /// テスト観点: プロジェクトのプロパティを変更した際に、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task UpdateProjectProperty_ShouldTriggerAutoSave()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("Old Name");

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { project });

        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, _serviceProviderMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var mainViewModel = new MainViewModel(loadUseCaseMock.Object, saveUseCaseMock.Object, findProjectUseCaseMock.Object, syncServiceMock.Object, addProjectUseCaseMock.Object, viewModelFactoryMock.Object, _mainLoggerMock.Object);
        await System.Threading.Tasks.Task.Delay(100);

        var projectViewModel = mainViewModel.Projects.First(p => p.Id == projectId);

        // Act
        projectViewModel.Name = "New Name";

        // Assert
        await System.Threading.Tasks.Task.Delay(500);
        saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p => p.Id == projectId && p.Name == "New Name")), Times.AtLeastOnce(), "プロジェクト�Eプロパティ変更時にリポジトリの SaveAsync が呼び出されること");
    }
}
