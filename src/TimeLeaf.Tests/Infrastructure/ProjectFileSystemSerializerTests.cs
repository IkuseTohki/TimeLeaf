using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class ProjectFileSystemSerializerTests
{
    private IProjectFileSystemSerializer _serializer = null!;

    [TestInitialize]
    public void Setup()
    {
        // まだ実装クラスがないので、ここでRed状態（コンパイルエラーまたは未実装例外）
        // _serializer = new JsonProjectFileSystemSerializer();
    }

    /// <summary>
    /// テスト観点: DTO が正しく JSON にシリアライズされること。
    /// とくに、Enum が文字列として出力され、日本語がエスケープされないことを確認する。
    /// </summary>
    [TestMethod]
    public void Serialize_ShouldReturnExpectedJson()
    {
        // Arrange
        var dto = new ProjectBasicDto("テストプロジェクト", ProjectStatus.InProgress, ProjectHealth.Healthy);
        _serializer = new JsonProjectFileSystemSerializer();

        // Act
        var json = _serializer.Serialize(dto);

        // Assert
        Assert.IsTrue(json.Contains("\"Status\": \"InProgress\""), "Enum が文字列でシリアライズされていること");
        Assert.IsTrue(json.Contains("テストプロジェクト"), "日本語がエスケープされずに含まれていること");
    }

    /// <summary>
    /// テスト観点: JSON から DTO へ正しくデシリアライズされること。
    /// </summary>
    [TestMethod]
    public void Deserialize_ShouldReturnExpectedDto()
    {
        // Arrange
        var json = "{\"Name\": \"Deserialized\", \"Status\": \"Completed\"}";
        _serializer = new JsonProjectFileSystemSerializer();

        // Act
        var dto = _serializer.Deserialize<ProjectBasicDto>(json);

        // Assert
        Assert.IsNotNull(dto);
        Assert.AreEqual("Deserialized", dto.Name);
        Assert.AreEqual(ProjectStatus.Completed, dto.Status);
    }
}
