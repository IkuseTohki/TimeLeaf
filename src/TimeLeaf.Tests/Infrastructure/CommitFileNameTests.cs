using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class CommitFileNameTests
{
    /// <summary>
    /// テスト観点: 各要素から、仕様書(ADR-0001)に準拠したファイル名が生成されることを確認する。
    /// 命名規則: {yyyyMMdd_HHmmss_fff}_{UserID}_{GUID}_{Category}.json
    /// </summary>
    [TestMethod]
    public void Generate_ShouldReturnCorrectFormat()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 21, 15, 30, 45, 123);
        var userId = "kiddy";
        var guid = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var category = "TaskBasic";

        // Act
        string fileName = CommitFileName.Generate(timestamp, userId, guid, category);

        // Assert
        Assert.AreEqual("20260221_153045_123_kiddy_123456781234123412341234567890ab_TaskBasic.json", fileName);
    }

    /// <summary>
    /// テスト観点: ファイル名から各属性が正しく抽出（パース）できることを確認する。
    /// </summary>
    [TestMethod]
    public void Parse_ShouldReturnCorrectAttributes()
    {
        // Arrange
        var fileName = "20260221_153045_123_kiddy_123456781234123412341234567890ab_TaskBasic.json";

        // Act
        var result = CommitFileName.Parse(fileName);

        // Assert
        Assert.AreEqual(new DateTime(2026, 2, 21, 15, 30, 45, 123), result.Timestamp);
        Assert.AreEqual("kiddy", result.UserId);
        Assert.AreEqual(Guid.Parse("12345678-1234-1234-1234-1234567890ab"), result.Guid);
        Assert.AreEqual("TaskBasic", result.Category);
    }

    /// <summary>
    /// テスト観点: ミリ秒まで含めたタイムスタンプにより、ファイル名が文字列ソートで時系列順になることを確認する。
    /// </summary>
    [TestMethod]
    public void Sorting_ShouldBeChronological()
    {
        // Arrange
        var baseTime = new DateTime(2026, 2, 21, 10, 0, 0);
        var list = new List<string>
        {
            CommitFileName.Generate(baseTime.AddMilliseconds(200), "user1", Guid.NewGuid(), "Cat"),
            CommitFileName.Generate(baseTime.AddMilliseconds(100), "user1", Guid.NewGuid(), "Cat"),
            CommitFileName.Generate(baseTime.AddMilliseconds(150), "user1", Guid.NewGuid(), "Cat")
        };

        // Act
        var sorted = list.OrderBy(x => x).ToList();

        // Assert
        Assert.Contains("100000_100", sorted[0]);
        Assert.Contains("100000_150", sorted[1]);
        Assert.Contains("100000_200", sorted[2]);
    }
}
