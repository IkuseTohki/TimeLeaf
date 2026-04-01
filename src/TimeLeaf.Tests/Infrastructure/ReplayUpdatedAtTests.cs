using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class ReplayUpdatedAtTests
{
    private string _tempDir = null!;
    private FolderProjectRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TimeLeaf_ReplayTest_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
        var loggerMock = new Mock<ILogger<FolderProjectRepository>>();
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        _repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, loggerMock.Object);
    }
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            foreach (var file in Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// テスト観点: 過去に保存されたプロジェクトをロード（Replay）した際、
    /// 最終更新日時（UpdatedAt）が「ロードした瞬間（今）」で上書きされず、
    /// 保存されていた過去の時刻が正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldPreservePastUpdatedAt_AndNotOverwriteWithNow()
    {
        // Arrange: 10日前の日付を持つファイルを直接作成する
        var projectId = Guid.NewGuid();
        var pastTime = DateTime.UtcNow.AddDays(-10);

        // フォルダ構成の作成
        var projectDir = Path.Combine(_tempDir, $"{projectId}_Test");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        // .project ファイル
        var meta = "{\"ProjectId\":\"" + projectId + "\", \"CreatedAt\":\"2026-01-01T00:00:00Z\", \"CreatedBy\":\"test\", \"SchemaVersion\":1}";
        await File.WriteAllTextAsync(Path.Combine(projectDir, ".project"), meta);

        // 過去の日時を持つ履歴ファイル
        var fileName = new DefaultCommitFileNameGenerator().Generate(pastTime, "user-A", "Project_Basic");
        var json = "{\"Name\":\"Old Project\", \"Status\":\"Initial\", \"HealthStatus\":\"Healthy\"}";
        await File.WriteAllTextAsync(Path.Combine(changesDir, fileName), json);

        // Act: ロード
        var loadedProject = await _repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);

        // 許容誤差（パースの精度）
        var diff = (loadedProject.UpdatedAt - pastTime).Duration();
        Assert.IsTrue(diff < TimeSpan.FromSeconds(1),
            $"UpdatedAt should be {pastTime}, but was {loadedProject.UpdatedAt} (Diff: {diff})");
    }

    /// <summary>
    /// テスト観点: JSONデータ本体に更新日時が含まれていない状態でロード（Replay）した際、
    /// 最後に適用された履歴ファイル（ファイル名）のタイムスタンプが 
    /// Project.UpdatedAt として正しく設定されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldDeriveUpdatedAtFromLatestFileName_WhenJsonHasNoTime()
    {
        // Arrange: 意図的に異なるタイムスタンプを持つ2つのファイルを直接作成する
        var projectId = Guid.NewGuid();
        var olderTime = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var newerTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        var projectDir = Path.Combine(_tempDir, $"{projectId}_MetadataSource");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        await File.WriteAllTextAsync(Path.Combine(projectDir, ".project"), "{\"ProjectId\":\"" + projectId + "\", \"CreatedAt\":\"2026-01-01T00:00:00Z\", \"CreatedBy\":\"test\", \"SchemaVersion\":1}");

        // 1. 10:00 のタイムスタンプを持つファイル
        var file1 = new DefaultCommitFileNameGenerator().Generate(olderTime, "user-A", "Project_Basic");
        await File.WriteAllTextAsync(Path.Combine(changesDir, file1), "{\"Name\":\"Old\"}");

        // 2. 12:00 のタイムスタンプを持つファイル
        var file2 = new DefaultCommitFileNameGenerator().Generate(newerTime, "user-A", "Task_Progress");
        var taskDir = Path.Combine(changesDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(taskDir);
        await File.WriteAllTextAsync(Path.Combine(taskDir, file2), "{\"Id\":\"" + Guid.NewGuid() + "\", \"Status\":\"InProgress\"}");

        // Act: ロード
        var loadedProject = await _repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);

        // 最終更新日時は「最後（最新）のファイル」である 12:00 になっているべき
        Assert.AreEqual(newerTime, loadedProject.UpdatedAt,
            "最終更新日時は最新の履歴ファイルのタイムスタンプから復元されるべき");
    }

    /// <summary>
    /// テスト観点: コメントのレコード（JSON）に作成日時が含まれていない状態でロード（Replay）した際、
    /// そのファイル名のタイムスタンプが Comment.CreatedAt として正しく設定されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldRestoreCommentCreatedAtFromFileMetadata()
    {
        // Arrange: 過去の日時を持つコメントファイルを直接作成する
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var commentTime = new DateTime(2026, 1, 1, 15, 0, 0, DateTimeKind.Utc);

        var projectDir = Path.Combine(_tempDir, $"{projectId}_CommentSource");
        var changesDir = Path.Combine(projectDir, "changes");
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        Directory.CreateDirectory(taskDir);

        await File.WriteAllTextAsync(Path.Combine(projectDir, ".project"), "{\"ProjectId\":\"" + projectId + "\", \"CreatedAt\":\"2026-01-01T00:00:00Z\", \"CreatedBy\":\"test\", \"SchemaVersion\":1}");

        // 1. タスクが必要なので作成
        var taskFile = new DefaultCommitFileNameGenerator().Generate(commentTime.AddMinutes(-1), "user-A", "Task_Planning");
        await File.WriteAllTextAsync(Path.Combine(taskDir, taskFile), "{\"Id\":\"" + taskId + "\", \"Name\":\"Test Task\"}");

        // 2. JSON内に CreatedAt を持たないコメントファイル
        var fileName = new DefaultCommitFileNameGenerator().Generate(commentTime, "user-A", "Comment");
        var json = "{\"Id\":\"" + commentId + "\", \"TaskId\":\"" + taskId + "\", \"AuthorId\":\"" + authorId + "\", \"Content\":\"Hello\", \"AttachmentLinks\":[]}";
        await File.WriteAllTextAsync(Path.Combine(taskDir, fileName), json);

        // Act: ロード
        var loadedProject = await _repository.LoadAsync(projectId);

        // Assert
        var comment = loadedProject?.Tasks.FirstOrDefault()?.Comments.FirstOrDefault();
        Assert.IsNotNull(comment, "コメントがロードされていること");

        // ファイル名の 15:00 が復元されているべき
        Assert.AreEqual(commentTime, comment.CreatedAt,
            "コメントの作成日時はファイル名のタイムスタンプから復元されるべき");
    }

    /// <summary>
    /// テスト観点: エンティティの各プロパティを変更しただけでは UpdatedAt は更新されず、
    /// SaveAsync によるディスクへの保存が成功したタイミングで、
    /// 保存されたタイムスタンプが Project.UpdatedAt に反映されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Save_ShouldUpdateUpdatedAtOnlyAfterSuccess()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Initial Name");
        var originalUpdatedAt = project.UpdatedAt;

        // 少し時間を置いてから名前を変更
        await Task.Delay(10);
        project.UpdateName("Changed Name");

        // Assert (Pre-Save): まだ UpdatedAt は変わっていないはず（新しい仕様）
        Assert.AreEqual(originalUpdatedAt, project.UpdatedAt, "保存前は最終更新日時は変更されないこと");

        // Act: 保存
        var startTime = DateTime.UtcNow;
        await _repository.SaveAsync(project, "user-A");
        var endTime = DateTime.UtcNow;

        // Assert (Post-Save): 保存成功後に、保存時のタイムスタンプで更新されていること
        Assert.AreNotEqual(originalUpdatedAt, project.UpdatedAt, "保存後に最終更新日時は更新されるべき");
        Assert.IsTrue(project.UpdatedAt >= startTime && project.UpdatedAt <= endTime,
            $"UpdatedAt ({project.UpdatedAt}) は保存期間中 ({startTime}～{endTime}) の時刻であるべき");
    }
}
