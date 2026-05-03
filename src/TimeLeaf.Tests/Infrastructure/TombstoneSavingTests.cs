using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class TombstoneSavingTests
{
    private string _tempDir = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<Microsoft.Extensions.Logging.ILogger<FolderProjectRepository>> _loggerMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
        _loggerMock = new Mock<Microsoft.Extensions.Logging.ILogger<FolderProjectRepository>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            // Repository が ReadOnly 属性を付与するため、削除前に解除する
            var files = Directory.GetFiles(_tempDir, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(_tempDir, true);
        }
    }

    [TestMethod]
    public async System.Threading.Tasks.Task RemoveTask_ShouldCreateTombstoneFile_ViaSaveAsync()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectName = "TestProject";
        var taskId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_{projectName}");
        var changesDir = Path.Combine(projectDir, "changes");
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        Directory.CreateDirectory(taskDir);

        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new Mock<IProjectStorageMonitor>().Object;
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        var project = new Project(Guid.NewGuid()) { Id = projectId };
        project.UpdateName(projectName);
        project.AddTask(new ProjectTask { Id = taskId });

        // Act
        project.RemoveTask(taskId);
        await repository.SaveAsync(project, _testUserId.ToString());

        // Assert
        var tombstoneFiles = Directory.GetFiles(taskDir, "*_Deleted.json", SearchOption.AllDirectories);
        Assert.AreEqual(1, tombstoneFiles.Length, "削除マーカーファイルが生成されること");
        Assert.AreEqual(0, project.DeletedTaskIds.Count, "保存後は削除済みリストがクリアされること");
    }

    [TestMethod]
    public async System.Threading.Tasks.Task MultipleRemoveTasks_ShouldCreateMultipleTombstones()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var projectName = "MultiDeleteProject";
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new Mock<IProjectStorageMonitor>().Object;
        var repository = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);

        var project = new Project(Guid.NewGuid()) { Id = projectId };
        project.UpdateName(projectName);
        project.AddTask(new ProjectTask { Id = id1 });
        project.AddTask(new ProjectTask { Id = id2 });

        // Act
        project.RemoveTask(id1);
        project.RemoveTask(id2);
        await repository.SaveAsync(project, _testUserId.ToString());

        // Assert
        var changesDir = Path.Combine(_tempDir, $"{projectId}_{projectName}", "changes");
        Assert.AreEqual(1, Directory.GetFiles(Path.Combine(changesDir, id1.ToString()), "*_Deleted.json").Length);
        Assert.AreEqual(1, Directory.GetFiles(Path.Combine(changesDir, id2.ToString()), "*_Deleted.json").Length);
    }
}
