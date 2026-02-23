using System;
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
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class OverviewViewModelTests
{
    /// <summary>
    /// テスト観点: プロジェクト追加コマンドを実行し、ダイアログで確定した際、リストにプロジェクトが追加されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddProject_ShouldAddProjectToList()
    {
        // Arrange
        var projects = new ObservableCollection<ProjectViewModel>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var dialogServiceMock = new Mock<IDialogService>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        var projectName = "Test Project";
        var projectDesc = "Test Description";

        // AddProjectViewModel のモックを準備
        var addProjectVm = new AddProjectViewModel();
        addProjectVm.Name = projectName;
        addProjectVm.Description = projectDesc;

        serviceProviderMock.Setup(x => x.GetService(typeof(AddProjectViewModel)))
            .Returns(addProjectVm);

        // ダイアログを表示して true (確定) を返すように設定
        dialogServiceMock.Setup(x => x.ShowDialogAsync(addProjectVm))
            .ReturnsAsync(true);

        // UseCaseのモックはエンティティを返し、ViewModelはそれをラップしてProjectsコレクションに追加する
        addProjectUseCaseMock.Setup(x => x.ExecuteAsync(
            projectName, projectDesc, It.IsAny<TimeLeaf.Models.Enums.ProjectStatus>(), It.IsAny<TimeLeaf.Models.Enums.ProjectHealth>()))
            .ReturnsAsync((string name, string desc, TimeLeaf.Models.Enums.ProjectStatus status, TimeLeaf.Models.Enums.ProjectHealth health) =>
                new Project { Name = name, Description = desc, Status = status, HealthStatus = health });

        var loggerMock = new Mock<ILogger<OverviewViewModel>>();
        var viewModel = new OverviewViewModel(
            projects,
            addProjectUseCaseMock.Object,
            dialogServiceMock.Object,
            serviceProviderMock.Object,
            loggerMock.Object);

        // Act
        await viewModel.AddProjectCommand.ExecuteAsync(null);

        // Assert
        Assert.AreEqual(1, viewModel.Projects.Count);
        var added = viewModel.Projects.First();
        Assert.AreEqual(projectName, added.Name);
        Assert.AreEqual(projectDesc, added.Description);
        addProjectUseCaseMock.Verify(x => x.ExecuteAsync(projectName, projectDesc, It.IsAny<TimeLeaf.Models.Enums.ProjectStatus>(), It.IsAny<TimeLeaf.Models.Enums.ProjectHealth>()), Times.Once);
    }
}
