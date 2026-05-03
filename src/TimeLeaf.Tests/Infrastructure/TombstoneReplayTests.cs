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
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class TombstoneReplayTests
{
    private string _tempDir = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
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
    /// テスト観点: Deleted カテゴリの Tombstone ファイルが存在する場合、
    /// そのタスクが読み込まれないことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task LoadAsync_ShouldSkipDeletedTask()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_Test");
        var changesDir = Path.Combine(projectDir, "changes");
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        Directory.CreateDirectory(taskDir);

        var baseTime = DateTime.UtcNow;
        var generator = new DefaultCommitFileNameGenerator();
        var serializer = new JsonProjectFileSystemSerializer();

        // 1. .project 作成
        await File.WriteAllTextAsync(
            Path.Combine(projectDir, ".project"),
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime })
        );

        // 2. タスク作成履歴
        var taskFile = generator.Generate(baseTime.AddSeconds(1), "user1", "Task_Planning");
        await File.WriteAllTextAsync(
            Path.Combine(taskDir, taskFile),
            JsonSerializer.Serialize(new { Id = taskId, Name = "Should Be Deleted" })
        );

        // 3. Tombstone (Deleted) ファイル作成
        var tombstoneFile = generator.Generate(baseTime.AddSeconds(2), "user1", "Deleted", taskId);
        await File.WriteAllTextAsync(Path.Combine(taskDir, tombstoneFile), "{}");

        var monitor = new Mock<IProjectStorageMonitor>().Object;
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        // Act
        var project = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(project);
        Assert.AreEqual(0, project.Tasks.Count, "論理削除されたタスクは読み込まれてはならない");
    }
}
