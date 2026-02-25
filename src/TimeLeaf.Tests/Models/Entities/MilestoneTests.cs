using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class MilestoneTests
{
    /// <summary>
    /// テスト観点: 同一の日付とラベルを持つマイルストーンは、値として等価であること。
    /// </summary>
    [TestMethod]
    public void Equality_ShouldBeTrue_ForSameValues()
    {
        // Arrange
        var date = new DateTime(2026, 1, 1);
        var label = "リリース";
        var m1 = new Milestone { Date = date, Label = label };
        var m2 = new Milestone { Date = date, Label = label };

        // Assert
        Assert.AreEqual(m1, m2, "同じ値を持つマイルストーンは等価であるべき");
        Assert.IsTrue(m1 == m2, "== 演算子でも等価であるべき");
    }

    /// <summary>
    /// テスト観点: 空のラベルでマイルストーンを作成しようとした場合、例外が送出されること。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldThrow_ForEmptyLabel()
    {
        // Act & Assert
        try
        {
            _ = new Milestone { Date = DateTime.Now, Label = "" };
            Assert.Fail("空のラベルは例外をスローすべき");
        }
        catch (ArgumentException)
        {
            // Success
        }
    }
}
