using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class CommitFileNameTests
{
    private ICommitFileNameGenerator _generator = new DefaultCommitFileNameGenerator();

    /// <summary>
    /// テスト観点: 各要素から、仕様書(ADR-0001)に準拠したファイル名が生成されることを確認する。
    /// 命名規則: {yyyyMMdd_HHmmss_fff}_{UserID}_{GUID}_{Category}.json
    /// </summary>
    [TestMethod]
    public void Generate_ShouldReturnCorrectFormat()
    {
        // Arrange
        // UTCとして明示的に作成
        var timestamp = new DateTime(2026, 2, 21, 15, 30, 45, 123, DateTimeKind.Utc);
        var userId = "kiddy";
        var guid = Guid.Parse("12345678-1234-1234-1234-1234567890ab");
        var category = "TaskBasic";

        // Act
        string fileName = _generator.Generate(timestamp, userId, guid, category);

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
        var result = _generator.Parse(fileName);

        // Assert
        // Parse は常に Kind=Utc で返す
        Assert.AreEqual(new DateTime(2026, 2, 21, 15, 30, 45, 123, DateTimeKind.Utc), result.Timestamp);
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
        var baseTime = new DateTime(2026, 2, 21, 10, 0, 0, DateTimeKind.Utc);
        var list = new List<string>
        {
            _generator.Generate(baseTime.AddMilliseconds(200), "user1", Guid.NewGuid(), "Cat"),
            _generator.Generate(baseTime.AddMilliseconds(100), "user1", Guid.NewGuid(), "Cat"),
            _generator.Generate(baseTime.AddMilliseconds(150), "user1", Guid.NewGuid(), "Cat")
        };

        // Act
        var sorted = list.OrderBy(x => x).ToList();

        // Assert
        StringAssert.Contains(sorted[0], "100000_100");
        StringAssert.Contains(sorted[1], "100000_150");
        StringAssert.Contains(sorted[2], "100000_200");
    }
}
