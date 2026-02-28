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
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class FolderProjectRepositoryReplayTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
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
    /// テスト観点: 複数の履歴ファイルがある場合、タイムスタンプが最新のものが最終的な状態として採用される(LWW)ことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task LoadAllAsync_ShouldApplyReplayWithLWW()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_InitialName");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var baseTime = new DateTime(2026, 2, 21, 10, 0, 0);

        // 0. .project メタデータの作成
        var metaFile = Path.Combine(projectDir, ".project");
        await File.WriteAllTextAsync(metaFile,
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime, SchemaVersion = 1 }));
        // 1. 古い変更 (Name = "Old Name")
        var oldFile = new DefaultCommitFileNameGenerator().Generate(baseTime, "user1", Guid.NewGuid(), "ProjectBasic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, oldFile),
            JsonSerializer.Serialize(new { Name = "Old Name" }));

        // 2. 新しい変更 (Name = "New Name")
        var newFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(1), "user1", Guid.NewGuid(), "ProjectBasic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, newFile),
            JsonSerializer.Serialize(new { Name = "New Name" }));
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        // Act
        var projects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(1, projects.Count);
        Assert.AreEqual(projectId, projects[0].Id);
        Assert.AreEqual("New Name", projects[0].Name, "最新のファイルの内容が反映されていること");
    }

    /// <summary>
    /// テスト観点: Description プロパティが Replay によって正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task LoadAllAsync_ShouldReplayDescription()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_DescTest");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var baseTime = DateTime.UtcNow;
        var metaFile = Path.Combine(projectDir, ".project");
        await File.WriteAllTextAsync(metaFile,
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime, SchemaVersion = 1 }));

        var commitFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(1), "user1", Guid.NewGuid(), "ProjectBasic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, commitFile),
            JsonSerializer.Serialize(new { Name = "Desc Test Project", Description = "Test Description" }));

        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        // Act
        var projects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(1, projects.Count);
        Assert.AreEqual("Test Description", projects[0].Description);
    }
}
