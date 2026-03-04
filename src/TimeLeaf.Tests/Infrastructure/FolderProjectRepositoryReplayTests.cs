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
        var oldFile = new DefaultCommitFileNameGenerator().Generate(baseTime, "user1", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, oldFile),
            JsonSerializer.Serialize(new { Name = "Old Name", Status = "Initial", HealthStatus = "Healthy" }));

        // 2. 新しい変更 (Name = "New Name")
        var newFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(1), "user1", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, newFile),
            JsonSerializer.Serialize(new { Name = "New Name", Status = "Initial", HealthStatus = "Healthy" }));
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

        var basicFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(1), "user1", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, basicFile),
            JsonSerializer.Serialize(new { Name = "Desc Test Project", Status = "InProgress", HealthStatus = "Healthy" }));

        var descFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(2), "user1", "Project_Description");
        await File.WriteAllTextAsync(Path.Combine(changesDir, descFile),
            JsonSerializer.Serialize(new { Description = "Test Description" }));

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

    /// <summary>
    /// テスト観点: プロジェクト、タスク、コメントが混在する複雑な履歴が正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task LoadAllAsync_ShouldReplayComplexHistory()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_ComplexTest");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var baseTime = new DateTime(2026, 2, 21, 12, 0, 0, DateTimeKind.Utc);
        var generator = new DefaultCommitFileNameGenerator();

        // 0. .project
        await File.WriteAllTextAsync(Path.Combine(projectDir, ".project"),
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime, SchemaVersion = 1 }));

        // 1. Project Basic
        var projectBasicFile = generator.Generate(baseTime.AddMinutes(1), "user1", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, projectBasicFile),
            JsonSerializer.Serialize(new { Name = "Complex Project", Status = "InProgress", HealthStatus = "Healthy" }));

        // 2. Task Planning
        var taskId = Guid.NewGuid();
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        Directory.CreateDirectory(taskDir);
        var taskPlanningFile = generator.Generate(baseTime.AddMinutes(2), "user1", "Task_Planning");
        await File.WriteAllTextAsync(Path.Combine(taskDir, taskPlanningFile),
            JsonSerializer.Serialize(new { Id = taskId, Name = "Task 1", Priority = "High", EstimatedCost = 5.0, Assignee = "alice" }));

        // 3. Task Progress
        var taskProgressFile = generator.Generate(baseTime.AddMinutes(3), "user1", "Task_Progress");
        await File.WriteAllTextAsync(Path.Combine(taskDir, taskProgressFile),
            JsonSerializer.Serialize(new { Id = taskId, Status = "InProgress", ActualStartDate = baseTime.AddMinutes(10), ActualCost = 1.0 }));

        // 4. Comment
        var commentId = Guid.NewGuid();
        var commentFile = generator.Generate(baseTime.AddMinutes(4), "user1", "Comment");
        await File.WriteAllTextAsync(Path.Combine(taskDir, commentFile),
            JsonSerializer.Serialize(new { Id = commentId, TaskId = taskId, AuthorId = "alice", Content = "Started task" }));

        var serializer = new JsonProjectFileSystemSerializer();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        // Act
        var project = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(project);
        Assert.AreEqual("Complex Project", project.Name);
        Assert.AreEqual(1, project.Tasks.Count);

        var task = project.Tasks.First();
        Assert.AreEqual("Task 1", task.Name);
        Assert.AreEqual(TimeLeaf.Models.Enums.TaskStatus.InProgress, task.Status);
        Assert.AreEqual(1, task.Comments.Count);
        Assert.AreEqual("Started task", task.Comments.First().Content);
        Assert.AreEqual(baseTime.AddMinutes(4), project.UpdatedAt, "最終更新日時が最後のファイルの時刻になっていること");
    }
}
