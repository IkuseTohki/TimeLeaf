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
public class EndToEndFlowTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ILogger<MainViewModel>> _mainLoggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project>());
        _mainLoggerMock = new Mock<ILogger<MainViewModel>>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(new Mock<ICurrentUserService>().Object);
    }

    /// <summary>
    /// テスト観点: プロジェクト作成からタスク追加、プロパティ変更までの一連の操作が、
    /// 正しくリポジトリの保存処理(SaveAsync)に繋がることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task CreateProjectAndAddTask_ShouldFlowToRepository()
    {
        // 1. Initialize MainViewModel
        var loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        var saveUseCase = new SaveProjectUseCase(_repositoryMock.Object);
        var addProjectUseCase = new AddProjectUseCase(_repositoryMock.Object);

        var mainVM = new MainViewModel(loadUseCase, saveUseCase, _repositoryMock.Object, addProjectUseCase, _mainLoggerMock.Object, _serviceProviderMock.Object);
        await System.Threading.Tasks.Task.Delay(100);

        // 2. Add a new project (Overview -> UseCase -> MainVM.Projects)
        var overviewVM = (OverviewViewModel)mainVM.CurrentViewModel;
        overviewVM.NewProjectName = "E2E Project";
        await overviewVM.AddProjectCommand.ExecuteAsync(null);

        var projectVM = mainVM.Projects.First(p => p.Name == "E2E Project");
        _repositoryMock.Verify(r => r.SaveAsync(projectVM.Model), Times.AtLeastOnce(), "プロジェクト作成時に保存されること");

        // 3. Navigate to Project
        mainVM.NavigateToProjectCommand.Execute(projectVM);
        var workspaceVM = (ProjectWorkspaceViewModel)mainVM.CurrentViewModel;

        // 4. Add a Task in Workspace
        workspaceVM.NewTaskName = "E2E Task";
        workspaceVM.AddTaskCommand.Execute(null);

        await System.Threading.Tasks.Task.Delay(500);
        _repositoryMock.Verify(r => r.SaveAsync(It.Is<Project>(p => p.Id == projectVM.Id && p.Tasks.Any(t => t.Name == "E2E Task"))), Times.AtLeastOnce(), "タスク追加時に保存されること");

        // 5. Update Task Property
        var taskVM = workspaceVM.Tasks.First(t => t.Name == "E2E Task");
        taskVM.Assignee = "E2E User";

        await System.Threading.Tasks.Task.Delay(500);
        _repositoryMock.Verify(r => r.SaveAsync(It.Is<Project>(p => p.Id == projectVM.Id && p.Tasks.Any(t => t.Assignee == "E2E User"))), Times.AtLeastOnce(), "タスクの属性変更時に保存されること");
    }
}
