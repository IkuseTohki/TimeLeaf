using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class OverviewViewModelTests
{
    /// <summary>
    /// テスト観点: プロジェクト名を入力し、追加コマンドを実行した際、リストにプロジェクトが追加されることを確認する。
    /// </summary>
    [TestMethod]
    public void AddProject_ShouldAddProjectToList()
    {
        // Arrange
        var projects = new ObservableCollection<Project>();
        var viewModel = new OverviewViewModel(projects);
        var projectName = "Test Project";
        var projectDesc = "Test Description";
        viewModel.NewProjectName = projectName;
        viewModel.NewProjectDescription = projectDesc;

        // Act
        viewModel.AddProjectCommand.Execute(null);

        // Assert
        Assert.HasCount(1, viewModel.Projects);
        var added = viewModel.Projects.First();
        Assert.AreEqual(projectName, added.Name);
        Assert.AreEqual(projectDesc, added.Description);
        Assert.AreEqual(string.Empty, viewModel.NewProjectName);
        Assert.AreEqual(string.Empty, viewModel.NewProjectDescription);
    }
}
