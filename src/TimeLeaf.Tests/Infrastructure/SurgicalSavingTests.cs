using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
public class SurgicalSavingTests
{
    private string _tempDir = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
        _loggerMock = new Mock<ILogger<FolderProjectRepository>>();
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
    /// 仕様観点: 変更があったプロジェクトのみがファイル生成の対象となること。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveProjectAsync_ShouldOnlyAffectTargetProject()
    {
        // Arrange
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        var repo = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);
        var projectA = new Project();
        projectA.UpdateName("ProjectA");
        var projectB = new Project();
        projectB.UpdateName("ProjectB");

        // Act
        // 拡張予定の個別保存メソッド（仮）を呼び出す想定
        // 現状の SaveAllAsync は全件保存してしまうため、このテストで不合格（Red）にする
        await repo.SaveAllAsync(new[] { projectA }, _testUserId.ToString());

        // Assert
        var projectAFolder = Path.Combine(_tempDir, $"{projectA.Id}_{projectA.Name}");
        var projectBFolder = Path.Combine(_tempDir, $"{projectB.Id}_{projectB.Name}");

        Assert.IsTrue(Directory.Exists(projectAFolder), "プロジェクトAのフォルダは作成されるべき");
        Assert.IsFalse(Directory.Exists(projectBFolder), "プロジェクトBのフォルダは作成されるべきではない");
    }

    /// <summary>
    /// 仕様観点: 内容に変更がない（前回スナップショットと同じ）場合は、新しいファイルを作成しないこと。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveProjectAsync_ShouldNotCreateFile_IfContentIsSame()
    {
        // Arrange
        var serializer = new JsonProjectFileSystemSerializer();
        var generator = new DefaultCommitFileNameGenerator();
        var monitor = new FileSystemProjectStorageMonitor(_tempDir, new Mock<ILogger<FileSystemProjectStorageMonitor>>().Object);
        var repo = new FolderProjectRepository(_tempDir, monitor, serializer, generator, _loggerMock.Object);
        var project = new Project();
        project.UpdateName("SameName");

        // 1回目の保存
        await repo.SaveAllAsync(new[] { project }, _testUserId.ToString());
        var projectDir = Directory.GetDirectories(_tempDir).First();
        var changesDir = Path.Combine(projectDir, "changes");
        var initialFileCount = Directory.GetFiles(changesDir).Length;

        // Act
        // 2回目の保存（内容は全く同じ）
        await repo.SaveAllAsync(new[] { project }, _testUserId.ToString());

        // Assert
        var currentFileCount = Directory.GetFiles(changesDir).Length;
        Assert.AreEqual(initialFileCount, currentFileCount, "内容が変わっていないため、新しいファイルは生成されないべき");
    }
}
