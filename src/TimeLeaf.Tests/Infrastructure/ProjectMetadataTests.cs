using System;
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
public class ProjectMetadataTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<ILogger<FolderProjectRepository>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("meta-user");
        _loggerMock = new Mock<ILogger<FolderProjectRepository>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// 仕様観点: 初回保存時に .project ファイルが作成され、ミリ秒精度の作成日時が記録されること。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAsync_ShouldCreateImmutableMetadataFile()
    {
        // Arrange
        var repo = new FolderProjectRepository(_tempDir, _loggerMock.Object);
        var project = new Project();
        project.UpdateName("MetaTest");

        // Act
        await repo.SaveAsync(project, "meta-user");

        // Assert
        var projectDir = Directory.GetDirectories(_tempDir).First();
        var metaFilePath = Path.Combine(projectDir, ".project");

        Assert.IsTrue(File.Exists(metaFilePath), ".project ファイルが生成されること");

        var metaJson = await File.ReadAllTextAsync(metaFilePath);
        using var doc = JsonDocument.Parse(metaJson);
        var createdAtStr = doc.RootElement.GetProperty("CreatedAt").GetString();

        // ミリ秒が含まれているかチェック (例: 2026-02-21T10:00:00.123Z)
        Assert.IsNotNull(createdAtStr);
        StringAssert.Contains(createdAtStr, ".", "作成日時にミリ秒が含まれていること");

        // 不変性の検証: 内容を変えて再保存しても .project は変わらないこと
        var firstMetaContent = metaJson;
        project.UpdateName("Changed Name");
        await System.Threading.Tasks.Task.Delay(10); // 時間をずらす
        await repo.SaveAsync(project, "meta-user");

        var secondMetaContent = await File.ReadAllTextAsync(metaFilePath);
        Assert.AreEqual(firstMetaContent, secondMetaContent, ".project ファイルの内容は不変であること");
    }

    /// <summary>
    /// 仕様観点: ロード時に .project の CreatedAt に基づいて時系列昇順でソートされることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task LoadAllAsync_ShouldSortByCreatedAt()
    {
        // Arrange
        var repo = new FolderProjectRepository(_tempDir, _loggerMock.Object);

        // 1つ目のプロジェクト作成
        var p1 = new Project();
        p1.UpdateName("First");
        await repo.SaveAsync(p1, "meta-user");

        await System.Threading.Tasks.Task.Delay(100); // 作成日をずらす

        // 2つ目のプロジェクト作成
        var p2 = new Project();
        p2.UpdateName("Second");
        await repo.SaveAsync(p2, "meta-user");

        // Act
        var result = (await repo.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.AreEqual("First", result[0].Name, "作成日が古いプロジェクトが先頭に来ること");
        Assert.AreEqual("Second", result[1].Name);
    }
}
