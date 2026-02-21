using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class SurgicalSavingTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// 仕様観点: 変更があったプロジェクトのみがファイル生成の対象となること。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveProjectAsync_ShouldOnlyAffectTargetProject()
    {
        // Arrange
        var repo = new FolderProjectRepository(_tempDir, _userServiceMock.Object);
        var projectA = new Project { Name = "ProjectA" };
        var projectB = new Project { Name = "ProjectB" };

        // Act
        // 拡張予定の個別保存メソッド（仮）を呼び出す想定
        // 現状の SaveAllAsync は全件保存してしまうため、このテストで不合格（Red）にする
        await repo.SaveAllAsync(new[] { projectA });

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
        var repo = new FolderProjectRepository(_tempDir, _userServiceMock.Object);
        var project = new Project { Name = "SameName" };

        // 1回目の保存
        await repo.SaveAllAsync(new[] { project });
        var projectDir = Directory.GetDirectories(_tempDir).First();
        var changesDir = Path.Combine(projectDir, "changes");
        var initialFileCount = Directory.GetFiles(changesDir).Length;

        // Act
        // 2回目の保存（内容は全く同じ）
        await repo.SaveAllAsync(new[] { project });

        // Assert
        var currentFileCount = Directory.GetFiles(changesDir).Length;
        Assert.AreEqual(initialFileCount, currentFileCount, "内容が変わっていないため、新しいファイルは生成されないべき");
    }
}
