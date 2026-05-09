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
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Infrastructure.Repositories;

[TestClass]
public class FolderProjectRepositoryTests
{
    private string _tempDir = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
        _loggerMock = new Mock<ILogger<FolderProjectRepository>>();
    }

    private FolderProjectRepository CreateRepository()
    {
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(
            _tempDir,
            new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object
        );
        return new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            // 読み取り専用属性がついていると削除に失敗するため、全ファイルの属性を解除する
            foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// テスト観点: プロジェクトを保存した際、仕様書通りのハイブリッドフォルダ構造（プロジェクトは直下、タスクはサブフォルダ）とファイルが生成されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAllAsync_ShouldCreateCorrectHybridFolderStructure()
    {
        // Arrange
        var repository = CreateRepository();
        var project = new Project(Guid.Empty);
        project.UpdateName("StructureTest");

        var task = new ProjectTask();
        task.UpdateName("SubFolderTask");
        project.AddTask(task);

        var container = new ProjectContainer(Guid.NewGuid(), "New Container");
        project.AddContainer(container);

        var projects = new List<Project> { project };

        // Act
        await repository.SaveAllAsync(projects, _testUserId.ToString());

        // Assert
        var expectedProjectDir = Directory
            .GetDirectories(_tempDir)
            .FirstOrDefault(d => d.Contains(project.Id.ToString()));
        Assert.IsNotNull(expectedProjectDir, "プロジェクトIDを含むディレクトリが作成されること");

        var changesDir = Path.Combine(expectedProjectDir, "changes");
        Assert.IsTrue(Directory.Exists(changesDir), "changes フォルダが作成されること");

        // プロジェクト直下に保存されるカテゴリの確認
        var rootFiles = Directory.GetFiles(changesDir);
        var basicFile = rootFiles.FirstOrDefault(f => f.Contains("Project_Basic"));
        Assert.IsNotNull(basicFile, "Project_Basic ファイルが changes 直下に出力されていること");

        // コンテナサブフォルダの確認
        var containerDir = Path.Combine(changesDir, container.Id.ToString());
        Assert.IsTrue(Directory.Exists(containerDir), "コンテナIDのサブフォルダが作成されること");

        // タスクサブフォルダの確認
        var taskDir = Path.Combine(changesDir, task.Id.ToString());
        Assert.IsTrue(Directory.Exists(taskDir), "タスクIDのサブフォルダが作成されること");
    }

    /// <summary>
    /// テスト観点: 保存された履歴ファイル（JSON）に、OSレベルの読み取り専用属性が付与されていることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAsync_ShouldSetReadOnlyAttribute()
    {
        // Arrange
        var repository = CreateRepository();
        var project = new Project(Guid.Empty);
        project.UpdateBasicInfo("ReadOnlyTest", ProjectStatus.InProgress);

        // Act
        await repository.SaveAsync(project, _testUserId.ToString());

        // Assert
        var projectDir = Path.Combine(_tempDir, $"{project.Id}_{project.Name}");
        var changesDir = Path.Combine(projectDir, "changes");
        var files = Directory.GetFiles(changesDir, "*.json", SearchOption.AllDirectories);

        Assert.IsTrue(files.Length > 0, "ファイルが出力されていること");
        foreach (var file in files)
        {
            var attributes = File.GetAttributes(file);
            Assert.IsTrue(
                attributes.HasFlag(FileAttributes.ReadOnly),
                $"ファイル {Path.GetFileName(file)} が読み取り専用属性を持っていること"
            );
        }
    }

    /// <summary>
    /// テスト観点: タスクのアサイン情報 (Assignee) と依存関係 (Dependencies) が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTaskAssignmentAndDependencies()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project(Guid.Empty) { Id = projectId };
        project.UpdateName("RelationTest");

        var depTaskId = Guid.NewGuid();
        var mainTask = new ProjectTask();
        mainTask.UpdateName("MainTask");
        mainTask.AssignTo("user123");
        mainTask.AddConstraint(new TaskConstraint(depTaskId));
        project.AddTask(mainTask);

        // Act
        await repository.SaveAsync(project, _testUserId.ToString());
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == mainTask.Id);
        Assert.IsNotNull(loadedTask);
        Assert.AreEqual("user123", loadedTask.Assignee, "Assignee が正しく復元されること");
        Assert.AreEqual(1, loadedTask.Constraints.Count, "Constraints の要素数が正しいこと");
    }

    /// <summary>
    /// テスト観点: 内容に変更がない（前回スナップショットと同じ）場合は、新しいファイルを作成しないこと。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAsync_ShouldNotCreateFile_IfContentIsSame()
    {
        // Arrange
        var repository = CreateRepository();
        var project = new Project(Guid.Empty);
        project.UpdateName("SameName");

        await repository.SaveAsync(project, _testUserId.ToString());
        var projectDir = Directory.GetDirectories(_tempDir).First();
        var changesDir = Path.Combine(projectDir, "changes");
        var initialFileCount = Directory.GetFiles(changesDir).Length;

        // Act
        await repository.SaveAsync(project, _testUserId.ToString());

        // Assert
        var currentFileCount = Directory.GetFiles(changesDir).Length;
        Assert.AreEqual(
            initialFileCount,
            currentFileCount,
            "内容が変わっていないため、新しいファイルは生成されないべき"
        );
    }

    /// <summary>
    /// テスト観点: Deleted カテゴリの Tombstone ファイルが存在する場合、そのタスクが読み込まれないことを確認する。
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

        await File.WriteAllTextAsync(
            Path.Combine(projectDir, ".project"),
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime })
        );

        var taskFile = generator.Generate(baseTime.AddSeconds(1), "user1", "Task_Planning");
        await File.WriteAllTextAsync(
            Path.Combine(taskDir, taskFile),
            JsonSerializer.Serialize(new { Id = taskId, Name = "Should Be Deleted" })
        );

        var tombstoneFile = generator.Generate(baseTime.AddSeconds(2), "user1", "Deleted", taskId);
        await File.WriteAllTextAsync(Path.Combine(taskDir, tombstoneFile), "{}");

        var repository = CreateRepository();

        // Act
        var project = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(project);
        Assert.AreEqual(0, project.Tasks.Count, "論理削除されたタスクは読み込まれてはならない");
    }

    /// <summary>
    /// テスト観点: 同一のコメントをプロジェクト保存処理で複数回連続して保存しようとした場合、
    /// 物理的なファイル競合(同名エラー)を起こさずにスキップされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task TrySaveCommentAsync_ShouldSkipRedundantSaveWithoutError()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project(Guid.Empty) { Id = projectId };
        var task = new ProjectTask();
        task.UpdateName("Persistent Task");
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            AuthorId = _testUserId,
            Content = "Test Comment",
            CreatedAt = DateTime.Now,
        };
        task.AddComment(comment);
        project.AddTask(task);

        // Act
        await repository.SaveAsync(project, _testUserId.ToString());
        var repository2 = CreateRepository();

        // Assert
        try
        {
            await repository2.SaveAsync(project, _testUserId.ToString());
        }
        catch (Exception ex)
        {
            Assert.Fail($"コメントの再保存時に例外が発生しました: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// テスト観点: FileSystemWatcher により、外部でファイルが作成された際に ProjectChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public async Task FileWatcher_ShouldTriggerEvent_OnNewFile()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_TestProject");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        Guid? notifiedId = null;
        var tcs = new TaskCompletionSource<bool>();

        repository.ProjectChanged += (id) =>
        {
            notifiedId = id;
            tcs.TrySetResult(true);
        };

        // Act
        var externalFile = Path.Combine(changesDir, "20260221_120000_000_otheruser_guid_Project_Basic.json");
        await File.WriteAllTextAsync(externalFile, "{}");

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000)) == tcs.Task;
        Assert.IsTrue(completed, "FileSystemWatcher イベントがタイムアウトまでに発火しなかった");
        Assert.AreEqual(projectId, notifiedId, "通知されたプロジェクトIDが一致すること");
    }

    /// <summary>
    /// テスト観点: 過去に保存されたプロジェクトをロード（Replay）した際、
    /// 最終更新日時（UpdatedAt）が正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldPreservePastUpdatedAt()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var pastTime = DateTime.Now.AddDays(-10);

        var projectDir = Path.Combine(_tempDir, $"{projectId}_Test");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var meta =
            "{\"ProjectId\":\""
            + projectId
            + "\", \"CreatedAt\":\"2026-01-01T00:00:00Z\", \"CreatedBy\":\"test\", \"SchemaVersion\":1}";
        await File.WriteAllTextAsync(Path.Combine(projectDir, ".project"), meta);

        var fileName = new DefaultCommitFileNameGenerator().Generate(pastTime, "user-A", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, fileName), "{\"Name\":\"Old Project\"}");

        // Act
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var diff = (loadedProject.UpdatedAt - pastTime).Duration();
        Assert.IsTrue(
            diff < TimeSpan.FromSeconds(1),
            $"UpdatedAt should be {pastTime}, but was {loadedProject.UpdatedAt}"
        );
    }

    /// <summary>
    /// テスト観点: 複数の履歴ファイルがある場合、タイムスタンプが最新のものが最終的な状態として採用される(LWW)ことを確認する。
    /// </summary>
    [TestMethod]
    public async Task LoadAllAsync_ShouldApplyReplayWithLWW()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_LWWTest");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var baseTime = new DateTime(2026, 2, 21, 10, 0, 0);

        await File.WriteAllTextAsync(
            Path.Combine(projectDir, ".project"),
            JsonSerializer.Serialize(new { ProjectId = projectId, CreatedAt = baseTime })
        );

        var oldFile = new DefaultCommitFileNameGenerator().Generate(baseTime, "user1", "Project_Basic");
        await File.WriteAllTextAsync(
            Path.Combine(changesDir, oldFile),
            JsonSerializer.Serialize(new { Name = "Old Name" })
        );

        var newFile = new DefaultCommitFileNameGenerator().Generate(baseTime.AddSeconds(1), "user1", "Project_Basic");
        await File.WriteAllTextAsync(
            Path.Combine(changesDir, newFile),
            JsonSerializer.Serialize(new { Name = "New Name" })
        );

        // Act
        var projects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(1, projects.Count);
        Assert.AreEqual("New Name", projects[0].Name);
    }
}
