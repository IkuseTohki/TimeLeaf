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
using TimeLeaf.Services;
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

    private FolderProjectRepository CreateRepository()
    {
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        return new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);
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
        var repository = CreateRepository();
        var project = new Project();
        project.UpdateName("StructureTest");
        var projects = new List<Project> { project };

        // Act
        await repository.SaveAllAsync(projects, "test-user");

        // Assert
        // 1. プロジェクトフォルダの存在確認
        var expectedProjectDir = Directory.GetDirectories(_tempDir).FirstOrDefault(d => d.Contains(project.Id.ToString()));
        Assert.IsNotNull(expectedProjectDir, "プロジェクトIDを含むディレクトリが作成されること");

        // 2. changes フォルダの存在確認
        var changesDir = Path.Combine(expectedProjectDir, "changes");
        Assert.IsTrue(Directory.Exists(changesDir), "changes フォルダが作成されること");

        // 3. 履歴ファイルの存在確認
        var files = Directory.GetFiles(changesDir);
        Assert.IsTrue(files.Any(), "履歴ファイル(JSON)が出力されること");

        var basicFile = files.FirstOrDefault(f => f.Contains("ProjectBasic"));
        Assert.IsNotNull(basicFile, "ProjectBasic ファイルが出力されていること");
        var generator = new DefaultCommitFileNameGenerator();
        var commitFile = generator.Parse(Path.GetFileName(basicFile));
        Assert.AreEqual("test-user", commitFile.UserId);
        Assert.AreEqual("ProjectBasic", commitFile.Category);

        // 4. 内容の確認
        var json = File.ReadAllText(basicFile);
        StringAssert.Contains(json, "StructureTest", "プロジェクト名がJSONに含まれていること");
    }

    /// <summary>
    /// テスト観点: タスクの新しい属性 (Deadline, EstimatedCost, ActualCost) が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTaskCostProperties()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("CostTest");
        var deadline = new DateTime(2026, 12, 31, 23, 59, 0);
        var task = new ProjectTask();
        task.UpdateName("CostTask");
        task.UpdateSchedule(null, deadline);
        task.UpdateEstimatedCost(10.5);
        task.UpdateActualCost(8.25);
        project.AddTask(task);

        // Act
        await repository.SaveAsync(project, "test-user");
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
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("RelationTest");

        var depTaskId = Guid.NewGuid();
        var mainTask = new ProjectTask();
        mainTask.UpdateName("MainTask");
        mainTask.AssignTo("user123");
        mainTask.Dependencies.Add(depTaskId);
        project.AddTask(mainTask);

        // Act
        await repository.SaveAsync(project, "test-user");
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == mainTask.Id);
        Assert.IsNotNull(loadedTask, "保存されたタスクがロードされること");
        Assert.AreEqual("user123", loadedTask.Assignee, "Assignee が正しく復元されること");
        Assert.AreEqual(1, loadedTask.Dependencies.Count, "Dependencies の要素数が正しいこと");
        Assert.AreEqual(depTaskId, loadedTask.Dependencies[0], "Dependencies の内容が正しいこと");
    }

    /// <summary>
    /// テスト観点: プロジェクトにマイルストーンを追加して保存し、再度ロードした際にマイルストーンが正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveMilestones()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("MilestoneTest");
        var mDate = new DateTime(2026, 10, 10);
        var mLabel = "Final Release";
        project.AddMilestone(new Milestone(mDate, mLabel));

        // Act
        await repository.SaveAsync(project, "test-user");
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        Assert.AreEqual(1, loadedProject.Milestones.Count, "ロードされたマイルストーンが1つであること");
        Assert.AreEqual(mDate, loadedProject.Milestones.First().Date, "マイルストーンの日付が一致すること");
        Assert.AreEqual(mLabel, loadedProject.Milestones.First().Label, "マイルストーンのラベルが一致すること");
    }

    /// <summary>
    /// テスト観点: タスクのスケジュール属性（予定開始、実績開始、実績終了）が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTaskScheduleProperties()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();
        var project = new Project { Id = projectId };
        project.UpdateName("ScheduleTest");
        var sDate = new DateTime(2026, 4, 1);
        var asDate = new DateTime(2026, 4, 2);
        var aeDate = new DateTime(2026, 4, 10);

        var task = new ProjectTask();
        task.UpdateName("ScheduleTask");
        task.UpdateSchedule(sDate, null);
        task.UpdateActualDates(asDate, aeDate);
        project.AddTask(task);

        // Act
        await repository.SaveAsync(project, "test-user");
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == task.Id);
        Assert.IsNotNull(loadedTask);
        Assert.AreEqual(sDate, loadedTask.ScheduledStartDate, "ScheduledStartDate が正しく復元されること");
        Assert.AreEqual(asDate, loadedTask.ActualStartDate, "ActualStartDate が正しく復元されること");
        Assert.AreEqual(aeDate, loadedTask.ActualEndDate, "ActualEndDate が正しく復元されること");
    }

    /// <summary>
    /// テスト観点: プロジェクトの作成日時 (CreatedAt) と最終更新日時 (UpdatedAt) が正しく保存・復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveTimeMetadata()
    {
        // Arrange
        var repository = CreateRepository();
        var projectId = Guid.NewGuid();

        var createdAt = new DateTime(2026, 2, 20, 10, 0, 0, DateTimeKind.Utc);

        var project = new Project(
            projectId,
            "MetadataTest",
            "",
            ProjectStatus.Initial,
            ProjectHealth.Healthy,
            createdAt,
            createdAt,
            null,
            null
        );
        // Act
        await repository.SaveAsync(project, "test-user");
        var savedUpdatedAt = project.UpdatedAt; // 保存によって確定した時刻
        var loadedProject = await repository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        Assert.AreEqual(createdAt, loadedProject.CreatedAt, "作成日時が一致すること");

        // 許容誤差（シリアライズ等によるわずかな差）
        var diff = (savedUpdatedAt - loadedProject.UpdatedAt).Duration();
        Assert.IsTrue(diff < TimeSpan.FromSeconds(1), $"最終更新日時が一致すること (Diff: {diff})");
    }
}
