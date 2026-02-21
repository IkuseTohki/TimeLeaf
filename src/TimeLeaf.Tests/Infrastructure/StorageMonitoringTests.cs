using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class StorageMonitoringTests
{
    private string _tempDir = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("watcher-test");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// 仕様観点: FileSystemWatcher により、外部でファイルが作成された際に ProjectChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task FileWatcher_ShouldTriggerEvent_OnNewFile()
    {
        // Arrange
        var repository = new FolderProjectRepository(_tempDir, _userServiceMock.Object);
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
        // 外部プロセスによるファイル作成をシミュレート
        var externalFile = Path.Combine(changesDir, "20260221_120000_000_otheruser_guid_ProjectBasic.json");
        await File.WriteAllTextAsync(externalFile, "{}");

        // Assert
        // OSのイベント通知を待機 (最大2秒)
        var completed = await System.Threading.Tasks.Task.WhenAny(tcs.Task, System.Threading.Tasks.Task.Delay(2000)) == tcs.Task;

        Assert.IsTrue(completed, "FileSystemWatcher イベントがタイムアウトまでに発火しなかった");
        Assert.AreEqual(projectId, notifiedId, "通知されたプロジェクトIDが一致すること");
    }
}
