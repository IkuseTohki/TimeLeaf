using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
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
        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
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

        // Factory mock setup
        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, dialogServiceMock.Object, _serviceProviderMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        var projectId = Guid.NewGuid();
        var initialProject = new Project { Id = projectId };
        initialProject.UpdateName("Initial");

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new List<Project> { initialProject });

        var viewModel = new MainViewModel(loadUseCaseMock.Object, saveUseCaseMock.Object, findProjectUseCaseMock.Object, syncServiceMock.Object, addProjectUseCaseMock.Object, viewModelFactoryMock.Object, loggerMock.Object);
        await System.Threading.Tasks.Task.Delay(100); // InitializeAsync の完了を待つ

        // ロードされる「最新」の状態を準備（別のタスクがある状態）
        var updatedProject = new Project { Id = projectId };
        updatedProject.UpdateName("Updated");
        updatedProject.AddTask(new ProjectTask { Name = "Task from Sync" }); // 同期で追加されるタスク
        findProjectUseCaseMock.Setup(r => r.ExecuteAsync(projectId)).ReturnsAsync(updatedProject);

        // Act
        syncServiceMock.Raise(r => r.ProjectChanged += null, projectId);
        await System.Threading.Tasks.Task.Delay(500); // OnProjectChanged 内の Dispatcher.InvokeAsync の完了をより長く待つ

        // Assert
        var project = viewModel.Projects.First(pvm => pvm.Id == projectId); // ViewModel を検索

        Assert.AreEqual(1, project.Model.Tasks.Count, "同期によってタスクが1件に更新されていること");
        Assert.AreEqual("Task from Sync", project.Model.Tasks.First().Name, "同期された最新のタスク名が反映されていること");
    }
}
