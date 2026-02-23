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
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class InfiniteLoopReproductionTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _dialogServiceMock = new Mock<IDialogService>();

        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(_userServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(new Mock<IAddProjectUseCase>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(_dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);
    }

    /// <summary>
    /// テスト観点: タスク追加（見積工数あり）後の同期再ロードが、
    /// 不要な再保存（無限ループの原因）を引き起こさないことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SyncAfterTaskAddition_ShouldNotTriggerRedundantSave()
    {
        // 1. Arrange
        var projectId = Guid.NewGuid();
        var projectEntity = new Project { Id = projectId, Name = "Test Project", UpdatedAt = DateTime.Now };
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { projectEntity });
        _repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(projectEntity);

        var loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        var saveUseCase = new SaveProjectUseCase(_repositoryMock.Object);
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();

        var mainVM = new MainViewModel(loadUseCase, saveUseCase, _repositoryMock.Object, addProjectUseCaseMock.Object, loggerMock.Object, _serviceProviderMock.Object);
        await Task.Delay(100); // Wait for initialize

        var projectVM = mainVM.Projects.First();
        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, _userServiceMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);

        // 2. Act - Add a task with estimated cost
        // これにより ViewModel の UpdatedAt が更新され、SaveAsync が呼ばれるはず
        workspaceVM.NewTaskName = "Task with Cost";
        workspaceVM.NewTaskEstimatedCost = 10.0;
        workspaceVM.AddTaskCommand.Execute(null);

        await Task.Delay(200); // SaveAsync の実行を待つ
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Project>()), Times.AtLeastOnce, "タスク追加により保存が走ること");

        // 3. Simulate Synced Reload (ProjectChanged event)
        // 実際のリポジトリではこのタイミングで replayed されたエンティティが返る
        var replayedProject = new Project { Id = projectId, Name = "Test Project", UpdatedAt = projectEntity.UpdatedAt };
        foreach (var t in projectEntity.Tasks) replayedProject.Tasks.Add(t);
        _repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(replayedProject);

        // 保存回数をリセットして、再ロードによって保存が走らないか監視
        _repositoryMock.Invocations.Clear();

        // プロジェクト変更イベントを発火
        _repositoryMock.Raise(r => r.ProjectChanged += null, projectId);

        await Task.Delay(500); // OnProjectChanged (Dispatcher.InvokeAsync) の完了を待つ

        // 4. Assert
        _repositoryMock.Verify(r => r.SaveAsync(It.IsAny<Project>()), Times.Never, "同期再ロードによって保存が走ってはいけない (無限ループの原因)");
    }

    /// <summary>
    /// テスト観点: 見積工数を持つタスクを複数追加した際、
    /// 安定した状態で保存が行われ、無限ループ（過剰な保存）に陥らないことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task AddingMultipleTasksWithCost_ShouldNotLoop()
    {
        // 1. Arrange
        var projectId = Guid.NewGuid();
        var projectEntity = new Project { Id = projectId, Name = "Loop Test" };
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { projectEntity });
        _repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(projectEntity);

        var loadUseCase = new LoadProjectsUseCase(_repositoryMock.Object);
        var saveUseCase = new SaveProjectUseCase(_repositoryMock.Object);
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();

        var mainVM = new MainViewModel(loadUseCase, saveUseCase, _repositoryMock.Object, addProjectUseCaseMock.Object, loggerMock.Object, _serviceProviderMock.Object);
        await Task.Delay(100);

        var projectVM = mainVM.Projects.First();
        var workspaceVM = new ProjectWorkspaceViewModel(projectVM, _userServiceMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);

        // 2. Act - Add 1st task
        workspaceVM.NewTaskName = "Task 1";
        workspaceVM.NewTaskEstimatedCost = 10.0;
        workspaceVM.AddTaskCommand.Execute(null);
        await Task.Delay(200);

        // 3. Act - Add 2nd task
        workspaceVM.NewTaskName = "Task 2";
        workspaceVM.NewTaskEstimatedCost = 20.0;
        workspaceVM.AddTaskCommand.Execute(null);
        await Task.Delay(200);

        // 4. Simulate Sync (often happens after save)
        _repositoryMock.Raise(r => r.ProjectChanged += null, projectId);
        await Task.Delay(500);

        // 5. Assert
        // 保存回数が異常に多くないか（各操作につき数回程度なら許容、無限なら数百回になる）
        var saveCount = _repositoryMock.Invocations.Count(i => i.Method.Name == "SaveAsync");
        Assert.IsTrue(saveCount < 10, $"Save count is too high ({saveCount}), possible loop detected.");
    }
}
