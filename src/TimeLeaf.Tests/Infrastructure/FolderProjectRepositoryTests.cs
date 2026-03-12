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
        var project = new Project();
        project.UpdateName("StructureTest");
        var task = new ProjectTask();
        task.UpdateName("SubFolderTask");
        project.AddTask(task);
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

        // 3. プロジェクト基本情報（直下）の存在確認
        var rootFiles = Directory.GetFiles(changesDir);
        var basicFile = rootFiles.FirstOrDefault(f => f.Contains("Project_Basic"));
        Assert.IsNotNull(basicFile, "Project_Basic ファイルが changes 直下に出力されていること");

        // ファイル名形式の確認: yyyyMMdd_HHmmss_fff_{UserID}_{Category}.json
        var fileName = Path.GetFileName(basicFile);
        var parts = fileName.Split('_');
        Assert.IsTrue(parts.Length >= 5, "ファイル名は少なくとも5つのパーツ（日付, 時刻, ミリ秒, ユーザーID, カテゴリ...）で構成されること");
        Assert.AreEqual("test-user", parts[3]);
        Assert.IsTrue(parts[4].StartsWith("Project"), "5番目以降のパーツはカテゴリ名であること");

        // 4. タスク情報（サブフォルダ）の存在確認
        var taskDir = Path.Combine(changesDir, task.Id.ToString());
        Assert.IsTrue(Directory.Exists(taskDir), "タスクIDのサブフォルダが作成されること");

        var taskFiles = Directory.GetFiles(taskDir);
        Assert.IsTrue(taskFiles.Any(f => f.Contains("Task_Planning")), "Task_Planning ファイルがタスクサブフォルダ内に出力されていること");
        Assert.IsTrue(taskFiles.Any(f => f.Contains("Task_Progress")), "Task_Progress ファイルがタスクサブフォルダ内に出力されていること");
    }

    /// <summary>
    /// テスト観点: 保存された履歴ファイル（JSON）に、OSレベルの読み取り専用属性が付与されていることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAsync_ShouldSetReadOnlyAttribute()
    {
        // Arrange
        var repository = CreateRepository();
        var project = new Project();
        project.UpdateBasicInfo("ReadOnlyTest", ProjectStatus.InProgress, ProjectHealth.Healthy);

        // Act
        await repository.SaveAsync(project, "test-user");

        // Assert
        var projectDir = Path.Combine(_tempDir, $"{project.Id}_{project.Name}");
        var changesDir = Path.Combine(projectDir, "changes");
        var files = Directory.GetFiles(changesDir, "*.json", SearchOption.AllDirectories);

        Assert.IsTrue(files.Length > 0, "ファイルが出力されていること");
        foreach (var file in files)
        {
            var attributes = File.GetAttributes(file);
            Assert.IsTrue(attributes.HasFlag(FileAttributes.ReadOnly), $"ファイル {Path.GetFileName(file)} が読み取り専用属性を持っていること");
        }
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
        Assert.IsNotNull(loadedTask.Dependencies);
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
        Assert.IsNotNull(loadedProject.Milestones);
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
