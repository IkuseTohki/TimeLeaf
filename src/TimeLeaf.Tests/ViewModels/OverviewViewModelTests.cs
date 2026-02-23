using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class OverviewViewModelTests
{
    /// <summary>
    /// テスト観点: プロジェクト名を入力し、追加コマンドを実行した際、リストにプロジェクトが追加されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddProject_ShouldAddProjectToList()
    {
        // Arrange
        var projects = new ObservableCollection<ProjectViewModel>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();

        // UseCaseのモックはエンティティを返し、ViewModelはそれをラップしてProjectsコレクションに追加する
        addProjectUseCaseMock.Setup(x => x.ExecuteAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TimeLeaf.Models.Enums.ProjectStatus>(), It.IsAny<TimeLeaf.Models.Enums.ProjectHealth>()))
            .ReturnsAsync((string name, string desc, TimeLeaf.Models.Enums.ProjectStatus status, TimeLeaf.Models.Enums.ProjectHealth health) =>
                new Project { Name = name, Description = desc, Status = status, HealthStatus = health });

        var loggerMock = new Mock<ILogger<OverviewViewModel>>();
        var viewModel = new OverviewViewModel(projects, addProjectUseCaseMock.Object, loggerMock.Object);
        var projectName = "Test Project";
        var projectDesc = "Test Description";
        viewModel.NewProjectName = projectName;
        viewModel.NewProjectDescription = projectDesc;

        // Act
        await viewModel.AddProjectCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual(1, viewModel.Projects.Count);
        var added = viewModel.Projects.First();
        Assert.AreEqual(projectName, added.Name);
        Assert.AreEqual(projectDesc, added.Description);
        Assert.AreEqual(string.Empty, viewModel.NewProjectName);
        Assert.AreEqual(string.Empty, viewModel.NewProjectDescription);
        addProjectUseCaseMock.Verify(x => x.ExecuteAsync(projectName, projectDesc, It.IsAny<TimeLeaf.Models.Enums.ProjectStatus>(), It.IsAny<TimeLeaf.Models.Enums.ProjectHealth>()), Times.Once);
    }
}
