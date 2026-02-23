using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class FolderProjectRepositoryTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
        _loggerMock = new Mock<ILogger<FolderProjectRepository>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// テスト観点: プロジェクトを保存した際、仕様書通りのフォルダ構造とファイルが生成されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAllAsync_ShouldCreateCorrectFolderStructure()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object, _loggerMock.Object); // ロガーモックを渡す
        var project = new Project { Name = "StructureTest" };
        var projects = new List<Project> { project };

        // Act
        await repository.SaveAllAsync(projects);

        // Assert
        // 1. プロジェクトフォルダの存在確認
        var expectedProjectDir = Directory.GetDirectories(_tempDir).FirstOrDefault(d => d.Contains(project.Id.ToString()));
        Assert.IsNotNull(expectedProjectDir, "プロジェクトIDを含むディレクトリが作成されること");

        // 2. changes フォルダの存在確認
        var changesDir = Path.Combine(expectedProjectDir, "changes");
        Assert.IsTrue(Directory.Exists(changesDir), "changes フォルダが作成されること");

        // 3. 履歴ファイルの存在確認
        var files = Directory.GetFiles(changesDir);
        Assert.IsNotEmpty(files, "履歴ファイル(JSON)が出力されること");

        var commitFile = CommitFileName.Parse(Path.GetFileName(files[0]));
        Assert.AreEqual("test-user", commitFile.UserId);
        Assert.AreEqual("ProjectBasic", commitFile.Category);

        // 4. 内容の確認
        var json = File.ReadAllText(files[0]);
        Assert.Contains("StructureTest", json, "プロジェクト名がJSONに含まれていること");
    }

    /// <summary>
    /// テスト観点: タスクの新しい属性 (Deadline, EstimatedCost, ActualCost) が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTaskCostProperties()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object, _loggerMock.Object); // ロガーモックを渡す
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "CostTest" };
        var deadline = new DateTime(2026, 12, 31, 23, 59, 0);
        var task = new ProjectTask
        {
            Name = "CostTask",
            Deadline = deadline,
            EstimatedCost = 10.5,
            ActualCost = 8.25
        };
        project.Tasks.Add(task);

        // Act
        await repository.SaveAsync(project);
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == task.Id);
        Assert.IsNotNull(loadedTask, "保存されたタスクがロードされること");
        Assert.AreEqual(task.Name, loadedTask.Name);
        Assert.AreEqual(deadline, loadedTask.Deadline, "Deadline が正しく復元されること");
        Assert.AreEqual(10.5, loadedTask.EstimatedCost, "EstimatedCost が正しく復元されること");
        Assert.AreEqual(8.25, loadedTask.ActualCost, "ActualCost が正しく復元されること");
    }

    /// <summary>
    /// テスト観点: タスクのアサイン情報 (Assignee) と依存関係 (Dependencies) が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTaskAssignmentAndDependencies()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object, _loggerMock.Object);
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId, Name = "RelationTest" };

        var depTaskId = Guid.NewGuid();
        var mainTask = new ProjectTask
        {
            Name = "MainTask",
            Assignee = "user123"
        };
        mainTask.Dependencies.Add(depTaskId);
        project.Tasks.Add(mainTask);

        // Act
        await repository.SaveAsync(project);
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == mainTask.Id);
        Assert.IsNotNull(loadedTask, "保存されたタスクがロードされること");
        Assert.AreEqual("user123", loadedTask.Assignee, "Assignee が正しく復元されること");
        Assert.HasCount(1, loadedTask.Dependencies, "Dependencies の要素数が正しいこと");
        Assert.AreEqual(depTaskId, loadedTask.Dependencies[0], "Dependencies の内容が正しいこと");
    }
}
