using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync())
                       .ReturnsAsync(new List<Project>());
    }

    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = new MainViewModel(_repositoryMock.Object);

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
        var viewModel = new MainViewModel(_repositoryMock.Object);

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
        var viewModel = new MainViewModel(_repositoryMock.Object);
        var project = new Project { Name = "New Project" };

        // Act
        viewModel.Projects.Add(project);

        // イベントハンドラの実行（非同期）を待機するため、少し待つ必要がある場合がある。
        // ここでは Moq の Verify を用いて、少なくとも1回（初期化以外で）呼ばれたか確認する。
        // 初期化で1回、追加で1回呼ばれるはず。
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
        var viewModel = new MainViewModel(_repositoryMock.Object);
        var project = new Project { Name = "Test Project" };

        // Act
        viewModel.NavigateToProjectCommand.Execute(project);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(ProjectWorkspaceViewModel));
    }
}
