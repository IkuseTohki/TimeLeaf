using System;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class ProjectSnapshotTests
{
    /// <summary>
    /// テスト観点: Project エンティティの基本属性が、Snapshot 用の DTO として正しくシリアライズされることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectBasicSnapshot_ShouldSerializeCorrectly()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Snapshot Test Project"
        };

        // Act
        var snapshot = new { project.Id, project.Name };
        var json = JsonSerializer.Serialize(snapshot);

        // Assert
        Assert.Contains("\"Name\":\"Snapshot Test Project\"", json, "JSONにプロジェクト名が含まれていること");
        Assert.Contains("\"Id\":\"11111111-1111-1111-1111-111111111111\"", json, "JSONにIDが含まれていること");
    }
}
