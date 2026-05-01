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
        var project = new Project(Guid.Empty);
        // IDはコンストラクタで生成されるが、テスト用にリフレクション等で上書きするか、
        // あるいは現在のIDを使用して検証する
        project.UpdateName("Snapshot Test Project");

        // Act
        var snapshot = new { project.Id, project.Name };
        var json = JsonSerializer.Serialize(snapshot);

        // Assert
        StringAssert.Contains(json, "\"Name\":\"Snapshot Test Project\"", "JSONにプロジェクト名が含まれていること");
        StringAssert.Contains(json, project.Id.ToString(), "JSONにIDが含まれていること");
    }
}
