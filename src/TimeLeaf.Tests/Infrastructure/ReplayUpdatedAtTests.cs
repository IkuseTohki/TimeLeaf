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
        _repository = new FolderProjectRepository(_tempDir, loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    /// <summary>
    /// テスト観点: 過去に保存されたプロジェクトをロード（Replay）した際、
    /// 最終更新日時（UpdatedAt）が「ロードした瞬間（今）」で上書きされず、
    /// 保存されていた過去の時刻が正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldPreservePastUpdatedAt_AndNotOverwriteWithNow()
    {
        // Arrange: 10日前の日付でプロジェクトを保存する
        var projectId = Guid.NewGuid();
        var pastTime = DateTime.UtcNow.AddDays(-10);
        var project = new Project { Id = projectId };
        project.UpdateName("Old Project");
        var task = new ProjectTask();
        task.UpdateName("Old Task");
        project.AddTask(task);
        project.SetUpdatedAt(pastTime); // 強制的に過去の時間をセット

        // 保存（内部で過去の時間を含むJSONが書き出される）
        await _repository.SaveAsync(project, "test-user");

        // Act: リポジトリを再生成してロード
        var loggerMock = new Mock<ILogger<FolderProjectRepository>>();
        var newRepository = new FolderProjectRepository(_tempDir, loggerMock.Object);
        var loadedProject = await newRepository.LoadAsync(projectId);

        // Assert
        Assert.IsNotNull(loadedProject);
        
        // 許容誤差（ミリ秒以下が切り捨てられる可能性があるため1秒以内とする）
        var diff = (loadedProject.UpdatedAt - pastTime).Duration();
        Assert.IsTrue(diff < TimeSpan.FromSeconds(1), 
            $"UpdatedAt should be close to {pastTime}, but was {loadedProject.UpdatedAt} (Diff: {diff})");
        
        Assert.IsTrue(loadedProject.UpdatedAt < DateTime.UtcNow.AddMinutes(-1), 
            "UpdatedAt should NOT be 'Just Now'.");
    }
}
