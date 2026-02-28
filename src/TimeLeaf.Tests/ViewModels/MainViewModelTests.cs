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
public class MainViewModelTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ILoadProjectsUseCase> _loadUseCaseMock = null!;
    private Mock<ISaveProjectUseCase> _saveSingleUseCaseMock = null!;
    private Mock<IFindProjectUseCase> _findProjectUseCaseMock = null!;
    private Mock<IProjectSyncService> _syncServiceMock = null!;
    private Mock<IAddProjectUseCase> _addProjectUseCaseMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        _saveSingleUseCaseMock = new Mock<ISaveProjectUseCase>();
        _findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        _syncServiceMock = new Mock<IProjectSyncService>();
        _addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _loggerMock = new Mock<ILogger<MainViewModel>>();

        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project>());

        _viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, _addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, new Mock<IServiceProvider>().Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var addTaskUseCaseMock = new Mock<IAddTaskUseCase>();
        var addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        var addMilestoneUseCaseMock = new Mock<IAddMilestoneUseCase>();
        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectWorkspaceViewModel(pvm, addTaskUseCaseMock.Object, addCommentUseCaseMock.Object, addMilestoneUseCaseMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object));
    }

    private MainViewModel CreateViewModel()
    {
        return new MainViewModel(
            _loadUseCaseMock.Object,
            _saveSingleUseCaseMock.Object,
            _findProjectUseCaseMock.Object,
            _syncServiceMock.Object,
            _addProjectUseCaseMock.Object,
            _viewModelFactoryMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(OverviewViewModel));
        _viewModelFactoryMock.Verify(x => x.CreateOverviewViewModel(viewModel.Projects), Times.Once);
    }

    /// <summary>
    /// テスト観点: アプリ起動時にリポジトリからプロジェクトがロードされることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldLoadProjectsFromRepository()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        _loadUseCaseMock.Verify(x => x.ExecuteAsync(), Times.Once);
    }

    /// <summary>
    /// テスト観点: 同期サービスからの変更通知が発生した際、対象プロジェクトが再ロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SyncEvent_ShouldTriggerReload()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var initialProject = new Project { Id = projectId };
        initialProject.UpdateName("Old Name");
        _loadUseCaseMock.Setup(x => x.ExecuteAsync()).ReturnsAsync(new List<Project> { initialProject });

        var viewModel = CreateViewModel();
        await Task.Delay(50);

        // ロードされる新しい状態を準備
        var updatedProject = new Project { Id = projectId };
        updatedProject.UpdateName("Updated Name");
        _findProjectUseCaseMock.Setup(x => x.ExecuteAsync(projectId)).ReturnsAsync(updatedProject);

        // Act
        _syncServiceMock.Raise(s => s.ProjectChanged += null, projectId);

        await System.Threading.Tasks.Task.Delay(200);

        // Assert
        var project = viewModel.Projects.First(p => p.Id == projectId);
        Assert.AreEqual("Updated Name", project.Name, "プロジェクト名が更新されてぁE��こと");
    }

    /// <summary>
    /// テスト観点: プロジェクトを選択した際、画面が ProjectWorkspaceViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project);

        var addTaskUseCaseMock = new Mock<IAddTaskUseCase>();
        var addCommentUseCaseMock = new Mock<IAddCommentUseCase>();
        var addMilestoneUseCaseMock = new Mock<IAddMilestoneUseCase>();
        var expectedWorkspace = new ProjectWorkspaceViewModel(projectViewModel, addTaskUseCaseMock.Object, addCommentUseCaseMock.Object, addMilestoneUseCaseMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(projectViewModel)).Returns(expectedWorkspace);

        // Act
        viewModel.NavigateToProjectCommand.Execute(projectViewModel);

        // Assert
        Assert.AreSame(expectedWorkspace, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateProjectWorkspaceViewModel(projectViewModel), Times.Once);
    }

    /// <summary>
    /// テスト観点: 戻るコマンドを実行した際、画面が OverviewViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateBack_ShouldSetOverviewViewModel()
    {
        // Arrange
        var viewModel = CreateViewModel();

        var expectedOverview = new OverviewViewModel(viewModel.Projects, _addProjectUseCaseMock.Object, new Mock<LeafKit.UI.Services.IDialogService>().Object, new Mock<IServiceProvider>().Object, new Mock<ILogger<OverviewViewModel>>().Object);
        _viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(viewModel.Projects)).Returns(expectedOverview);

        // Act
        viewModel.NavigateBackCommand.Execute(null);

        // Assert
        Assert.AreSame(expectedOverview, viewModel.CurrentViewModel);
        _viewModelFactoryMock.Verify(x => x.CreateOverviewViewModel(viewModel.Projects), Times.AtLeastOnce);
    }
}
