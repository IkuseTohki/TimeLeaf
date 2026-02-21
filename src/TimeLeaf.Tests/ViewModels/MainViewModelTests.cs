using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelTests
{
    /// <summary>
    /// テスト観点: アプリ起動時に初期画面として OverviewViewModel が設定されていることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldSetOverviewViewModelAsInitialPage()
    {
        // Act
        var viewModel = new MainViewModel();

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(OverviewViewModel));
    }

    /// <summary>
    /// テスト観点: プロジェクトを選択した際、画面が ProjectWorkspaceViewModel に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void NavigateToProject_ShouldSetProjectWorkspaceViewModel()
    {
        // Arrange
        var viewModel = new MainViewModel();
        var project = new Project { Name = "Test Project" };

        // Act
        viewModel.NavigateToProjectCommand.Execute(project);

        // Assert
        Assert.IsInstanceOfType(viewModel.CurrentViewModel, typeof(ProjectWorkspaceViewModel));
    }
}
