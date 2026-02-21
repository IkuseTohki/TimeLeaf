using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class MainViewModelSyncTests
{
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

        var projectId = Guid.NewGuid();
        var initialProject = new Project { Id = projectId, Name = "Initial" };
        initialProject.Tasks.Add(new TimeLeaf.Models.Entities.Task { Name = "Task 1" });

        repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { initialProject });

        var viewModel = new MainViewModel(loadUseCase, saveUseCase, repositoryMock.Object);
        await System.Threading.Tasks.Task.Delay(100); // 初期ロード待ち

        // ロードされる「最新」の状態を準備（別のタスクがある状態）
        var updatedProject = new Project { Id = projectId, Name = "Updated" };
        updatedProject.Tasks.Add(new TimeLeaf.Models.Entities.Task { Name = "Task from Sync" });
        repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(updatedProject);

        // Act
        // 外部変更イベントを発火
        repositoryMock.Raise(r => r.ProjectChanged += null, projectId);
        await System.Threading.Tasks.Task.Delay(300); // 同期処理待ち

        // Assert
        var project = viewModel.Projects.First(p => p.Id == projectId);

        // 現状のバグがあれば、ここが 0 件になったり、古いデータになったりする
        Assert.HasCount(1, project.Tasks, "同期によってタスクが1件に更新されていること");
        Assert.AreEqual("Task from Sync", project.Tasks.First().Name, "同期された最新のタスク名が反映されていること");
    }
}
