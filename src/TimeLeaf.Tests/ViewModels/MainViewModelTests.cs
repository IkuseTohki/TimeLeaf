using System.Collections.Generic;
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
    private SaveProjectsUseCase _saveUseCase = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync())
                       .ReturnsAsync(new List<Project>());

        _loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        _saveUseCase = new SaveProjectsUseCase(_repositoryMock.Object);
    }

    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = new MainViewModel(_loadUseCase, _saveUseCase);

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
        var viewModel = new MainViewModel(_loadUseCase, _saveUseCase);

        // Assert
        _repositoryMock.Verify(r => r.LoadAllAsync(), Times.Once);
    }

    /// <summary>
    /// テスト観点: プロジェクトが追加された際、リポジトリへの保存が実行されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddProject_ShouldTriggerSave()
    {
        // Arrange
        var viewModel = new MainViewModel(_loadUseCase, _saveUseCase);
        var project = new Project { Name = "New Project" };

        // Act
        viewModel.Projects.Add(project);

        // 非同期実行待ち
        await System.Threading.Tasks.Task.Delay(100);

        // Assert
        _repositoryMock.Verify(r => r.SaveAllAsync(It.IsAny<IEnumerable<Project>>()), Times.Once);
    }

    /// <summary>
    /// テスト観点: プロジェクトを選択した際、画面が ProjectWorkspaceViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel(_loadUseCase, _saveUseCase);
        var project = new Project { Name = "Test Project" };

        // Act
        viewModel.NavigateToProjectCommand.Execute(project);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(ProjectWorkspaceViewModel));
    }
}
