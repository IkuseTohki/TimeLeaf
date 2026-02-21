using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync())
                       .ReturnsAsync(new List<Project>());

        _loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        _saveSingleUseCase = new SaveProjectUseCase(_repositoryMock.Object);
    }

    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object);

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
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object);

        // Assert
        _repositoryMock.Verify(r => r.LoadAllAsync(), Times.Once);
    }

    /// <summary>
    /// テスト観点: プロジェクトが追加された際、そのプロジェクトの保存が実行されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddProject_ShouldTriggerSaveForThatProject()
    {
        // Arrange
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object);
        var project = new Project { Name = "New Project" };

        // Act
        viewModel.Projects.Add(project);

        // 非同期実行待ち
        await System.Threading.Tasks.Task.Delay(100);

        // Assert
        _repositoryMock.Verify(r => r.SaveAsync(project), Times.Once);
    }

    /// <summary>
    /// テスト観点: リポジトリの ProjectChanged イベントが発生した際、対象プロジェクトが再ロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ProjectChangedEvent_ShouldTriggerReload()
    {
        // Arrange
        var project = new Project { Name = "Old Name" };
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object);
        viewModel.Projects.Add(project);

        // ロードされる新しい状態を準備
        var updatedProject = new Project { Id = project.Id, Name = "Updated Name" };
        _repositoryMock.Setup(r => r.LoadAsync(project.Id)).ReturnsAsync(updatedProject);

        // Act
        // イベントを発火させる
        _repositoryMock.Raise(r => r.ProjectChanged += null, project.Id);

        // 非同期処理（Dispatcher.Invoke相当）の完了を待機
        // 単体テスト環境では Dispatcher がないので直列実行されるが、InvokeAsync 対策で待機
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
        var viewModel = new MainViewModel(_loadUseCase, _saveSingleUseCase, _repositoryMock.Object);
        var project = new Project { Name = "Test Project" };

        // Act
        viewModel.NavigateToProjectCommand.Execute(project);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(ProjectWorkspaceViewModel));
    }
}
