using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class ProjectAssignmentReplayTests
{
    private string _testRoot = null!;
    private string _changesDir = null!;
    private JsonProjectFileSystemSerializer _serializer = null!;
    private DefaultCommitFileNameGenerator _fileNameGenerator = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "TimeLeafTests", Guid.NewGuid().ToString());
        _changesDir = Path.Combine(_testRoot, "changes");
        Directory.CreateDirectory(_changesDir);
        _serializer = new JsonProjectFileSystemSerializer();
        _fileNameGenerator = new DefaultCommitFileNameGenerator();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    /// <summary>
    /// テスト観点: Project_Members ファイルからアサイン情報が正しく復元されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Replay_ShouldRestoreAssignments()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var userId = "user1";
        var assignedUser1 = Guid.NewGuid();
        var assignedUser2 = Guid.NewGuid();
        var dto = new ProjectMembersDto
        {
            AssignedUserIds = new List<Guid> { assignedUser1, assignedUser2 },
        };

        var fileName = _fileNameGenerator.Generate(DateTime.Now, userId, "Project_Members");
        await File.WriteAllTextAsync(Path.Combine(_changesDir, fileName), _serializer.Serialize(dto));

        var replayer = new ProjectHistoryReplayer(_serializer, _fileNameGenerator, NullLogger.Instance, new());

        // Act
        var project = await replayer.ReplayAsync(_changesDir, projectId, DateTime.Now, Guid.Empty);

        // Assert
        Assert.AreEqual(2, project.AssignedUserIds.Count);
        Assert.IsTrue(project.AssignedUserIds.Contains(assignedUser1));
        Assert.IsTrue(project.AssignedUserIds.Contains(assignedUser2));
    }
}
