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

using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelSyncTests
{
    private Mock<IServiceProvider> _serviceProviderMock = null!;

    /// <summary>
    /// テスト観点: 外部からの変更通知(ProjectChanged)による再ロード時に、
    /// 再度保存(SaveAsync)が走り、データが壊れないことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SyncReload_ShouldNotTriggerRedundantSave()
    {
        // Arrange
        var repositoryMock = new Mock<IProjectRepository>();
        var loadUseCase = new LoadProjectsUseCase(repositoryMock.Object);
        var saveUseCase = new SaveProjectUseCase(repositoryMock.Object);
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();
        var dialogServiceMock = new Mock<IDialogService>();
        _serviceProviderMock = new Mock<IServiceProvider>();

        // ProjectWorkspaceViewModel と OverviewViewModel 用のロガーもモックする
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(addProjectUseCaseMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);

        var projectId = Guid.NewGuid();
        var initialProject = new Project { Id = projectId };
        initialProject.UpdateName("Initial");
        // initialProject.Tasks.Add(new ProjectTask { Name = "Task 1" }); // 初期ロードではタスクを持たない

        repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { initialProject });

        var viewModel = new MainViewModel(loadUseCase, saveUseCase, repositoryMock.Object, addProjectUseCaseMock.Object, loggerMock.Object, _serviceProviderMock.Object);
        await System.Threading.Tasks.Task.Delay(100); // InitializeAsync の完了を待つ

        // ロードされる「最新」の状態を準備（別のタスクがある状態）
        var updatedProject = new Project { Id = projectId };
        updatedProject.UpdateName("Updated");
        updatedProject.AddTask(new ProjectTask { Name = "Task from Sync" }); // 同期で追加されるタスク
        repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(updatedProject);

        // Act
        repositoryMock.Raise(r => r.ProjectChanged += null, projectId);
        await System.Threading.Tasks.Task.Delay(500); // OnProjectChanged 内の Dispatcher.InvokeAsync の完了をより長く待つ

        // Assert
        var project = viewModel.Projects.First(pvm => pvm.Id == projectId); // ViewModel を検索

        Assert.AreEqual(1, project.Model.Tasks.Count, "同期によってタスクが1件に更新されていること");
        Assert.AreEqual("Task from Sync", project.Model.Tasks.First().Name, "同期された最新のタスク名が反映されていること");
    }
}
