using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class FileSystemProjectStorageMonitorTests
{
    private string _tempDir = null!;
    private Mock<ILogger<FileSystemProjectStorageMonitor>> _loggerMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _loggerMock = new Mock<ILogger<FileSystemProjectStorageMonitor>>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// テスト観点: 監視対象フォルダ内にプロジェクトファイルが作成された際、イベントが発火すること。
    /// </summary>
    [TestMethod]
    public async Task ProjectChanged_ShouldFire_WhenFileCreated()
    {
        // Arrange
        using var monitor = new FileSystemProjectStorageMonitor(_tempDir, _loggerMock.Object);
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_TestProject");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        Guid? notifiedId = null;
        var tcs = new TaskCompletionSource<bool>();
        monitor.ProjectChanged += (id) =>
        {
            notifiedId = id;
            tcs.TrySetResult(true);
        };

        // Act
        var externalFile = Path.Combine(changesDir, "20260221_120000_000_otheruser_guid_ProjectBasic.json");
        await File.WriteAllTextAsync(externalFile, "{}");

        // Assert
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(2000)) == tcs.Task;
        Assert.IsTrue(completed, "イベントが発火しなかった。");
        Assert.AreEqual(projectId, notifiedId);
    }

    /// <summary>
    /// テスト観点: MarkFileAsJustWritten で登録されたファイル名の変更は無視されること。
    /// </summary>
    [TestMethod]
    public async Task ProjectChanged_ShouldNotFire_WhenFileIsMarkedAsJustWritten()
    {
        // Arrange
        using var monitor = new FileSystemProjectStorageMonitor(_tempDir, _loggerMock.Object);
        var projectId = Guid.NewGuid();
        var projectDir = Path.Combine(_tempDir, $"{projectId}_TestProject");
        var changesDir = Path.Combine(projectDir, "changes");
        Directory.CreateDirectory(changesDir);

        var fileName = "20260221_120000_000_me_guid_ProjectBasic.json";
        monitor.MarkFileAsJustWritten(fileName);

        bool fired = false;
        monitor.ProjectChanged += (id) => fired = true;

        // Act
        var myFile = Path.Combine(changesDir, fileName);
        await File.WriteAllTextAsync(myFile, "{}");
        await Task.Delay(500); // 監視イベントが来るのを少し待つ

        // Assert
        Assert.IsFalse(fired, "自前で書き込んだファイルに対してイベントが発火してしまった。");
    }
}
