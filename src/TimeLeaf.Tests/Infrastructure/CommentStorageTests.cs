using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class CommentStorageTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("user-A");
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
    /// テスト観点: タスクにコメントを追加して保存した際、
    /// コメントが個別のファイル（蓄積型）として保存され、正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task SaveAndLoad_ShouldPreserveComments()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object, _loggerMock.Object);
        var project = new Project();
        project.UpdateName("CommentTestProject");
        var task = new ProjectTask { Name = "Task with Comment" };
        project.AddTask(task);

        var comment = new Comment
        {
            TaskId = task.Id,
            AuthorId = "user-A",
            Content = "First Comment",
            CreatedAt = new DateTime(2026, 2, 23, 10, 0, 0)
        };
        task.AddComment(comment);

        // Act
        await repository.SaveAsync(project);
        var loadedProject = await repository.LoadAsync(project.Id);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.FirstOrDefault(t => t.Id == task.Id);
        Assert.IsNotNull(loadedTask);
        Assert.AreEqual(1, loadedTask.Comments.Count);
        Assert.AreEqual("First Comment", loadedTask.Comments[0].Content);
        Assert.AreEqual("user-A", loadedTask.Comments[0].AuthorId);

        // 物理ファイルの確認
        var projectDir = Path.Combine(_tempDir, $"{project.Id}_{project.Name}");
        var changesDir = Path.Combine(projectDir, "changes");
        var files = Directory.GetFiles(changesDir, "*_Comment.json");
        Assert.AreEqual(1, files.Length, "コメントファイルが1つ出力されていること");
    }

    /// <summary>
    /// テスト観点: 同一タスクに複数のコメントがある場合、すべてが蓄積され復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task SaveAndLoad_MultipleComments_ShouldIncrementalAccumulate()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object, _loggerMock.Object);
        var project = new Project();
        project.UpdateName("MultiCommentProject");
        var task = new ProjectTask { Name = "Task" };
        project.AddTask(task);

        var c1 = new Comment { TaskId = task.Id, Content = "C1", CreatedAt = DateTime.Now.AddMinutes(-5) };
        var c2 = new Comment { TaskId = task.Id, Content = "C2", CreatedAt = DateTime.Now };
        task.AddComment(c1);
        task.AddComment(c2);

        // Act
        await repository.SaveAsync(project);
        var loadedProject = await repository.LoadAsync(project.Id);

        // Assert
        Assert.IsNotNull(loadedProject);
        var loadedTask = loadedProject.Tasks.First();
        Assert.AreEqual(2, loadedTask.Comments.Count);

        // 物理ファイルの確認
        var projectDir = Path.Combine(_tempDir, $"{project.Id}_{project.Name}");
        var changesDir = Path.Combine(projectDir, "changes");
        var files = Directory.GetFiles(changesDir, "*_Comment.json");
        Assert.AreEqual(2, files.Length, "コメントファイルが2つ独立して出力されていること");
    }
}
