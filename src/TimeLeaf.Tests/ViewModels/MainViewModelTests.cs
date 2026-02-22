using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private LoadProjectsUseCase _loadUseCase = null!;
    private SaveProjectUseCase _saveSingleUseCase = null!;
    private Mock<IAddProjectUseCase> _addProjectUseCaseMock = null!;
    private Mock<ILogger<MainViewModel>> _loggerMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync())
                       .ReturnsAsync(new List<Project>());
        _repositoryMock.Setup(r => r.LoadAsync(It.IsAny<Guid>()))
                       .ReturnsAsync((Guid id) => new Project { Id = id, Name = "Loaded Project" });

        _loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        _saveSingleUseCase = new SaveProjectUseCase(_repositoryMock.Object);
        _addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        _loggerMock = new Mock<ILogger<MainViewModel>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);


        _addProjectUseCaseMock.Setup(x => x.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeLeaf.Models.Enums.ProjectStatus>(), It.IsAny<TimeLeaf.Models.Enums.ProjectHealth>()))
            .ReturnsAsync((string name, string desc, TimeLeaf.Models.Enums.ProjectStatus status, TimeLeaf.Models.Enums.ProjectHealth health) =>
            new Project { Name = name, Description = desc, Status = status, HealthStatus = health });
    }

    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object); // serviceProvider を渡す

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(OverviewViewModel));
    }

    /// <summary>
    /// テスト観点: アプリ起動時にリポジトリからプロジェクトがロードされることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldLoadProjectsFromRepository()
    {
        // Act
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object); // serviceProvider を渡す

        // Assert
        _repositoryMock.Verify(r => r.LoadAllAsync(), Times.Once);
    }

    /// <summary>
    /// テスト観点: プロジェクトが追加された際、そのプロジェクトの保存が実行されることを確認する。
    /// （このテストはOverviewViewModelがAddProjectUseCaseを呼び出すようになったため、意味がなくなる。
    /// 今後はOverviewViewModelTestsでユースケースの呼び出しを検証すべき。一旦コメントアウト）
    /// </summary>
    // [TestMethod]
    // public async System.Threading.Tasks.Task AddProject_ShouldTriggerSaveForThatProject()
    // {
    //     // Arrange
    //     var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object);
    //     var project = new Project { Name = "New Project" };

    //     // Act
    //     viewModel.Projects.Add(new ProjectViewModel(project)); // ProjectViewModel でラップ

    //     // 非同期実行待ち
    //     await System.Threading.Tasks.Task.Delay(100);

    //     // Assert
    //     _repositoryMock.Verify(r => r.SaveAsync(project), Times.Once);
    // }

    /// <summary>
    /// テスト観点: リポジトリの ProjectChanged イベントが発生した際、対象プロジェクトが再ロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ProjectChangedEvent_ShouldTriggerReload()
    {
        // Arrange
        var project = new Project { Id = Guid.NewGuid(), Name = "Old Name" };
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { project });
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object); // serviceProvider を渡す

        // ロードされる新しい状態を準備
        var updatedProject = new Project { Id = project.Id, Name = "Updated Name" };
        _repositoryMock.Setup(r => r.LoadAsync(project.Id)).ReturnsAsync(updatedProject);

        // Act
        _repositoryMock.Raise(r => r.ProjectChanged += null, project.Id);

        await System.Threading.Tasks.Task.Delay(200);

        // Assert
        var currentProject = viewModel.Projects.First(p => p.Id == project.Id);
        Assert.AreEqual("Updated Name", currentProject.Name, "プロジェクト名が更新されていること");
    }

    /// <summary>
    /// テスト観点: プロジェクトを選択した際、画面が ProjectWorkspaceViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object); // serviceProvider を渡す
        var projectViewModel = new ProjectViewModel(new Project { Name = "Test Project" });

        // Act
        viewModel.NavigateToProjectCommand.Execute(projectViewModel);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(ProjectWorkspaceViewModel));
    }

    /// <summary>
    /// テスト観点: 戻るコマンドを実行した際、画面が OverviewViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateBack_ShouldSetOverviewViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object, _addProjectUseCaseMock.Object, _loggerMock.Object, _serviceProviderMock.Object); // serviceProvider を渡す
        viewModel.NavigateToProjectCommand.Execute(new ProjectViewModel(new Project { Name = "Some Project" }));

        // Act
        viewModel.NavigateBackCommand.Execute(null);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(OverviewViewModel));
    }
}
