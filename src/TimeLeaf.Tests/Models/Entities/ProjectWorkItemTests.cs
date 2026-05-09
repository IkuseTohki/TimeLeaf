using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectWorkItemTests
{
    private class TestWorkItem : ProjectWorkItem { }

    /// <summary>
    /// テスト観点: アイテム名が正しく設定・更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void Name_ShouldBeUpdatable()
    {
        // Arrange
        var item = new TestWorkItem();
        var newName = "Test Item";

        // Act
        item.UpdateName(newName);

        // Assert
        Assert.AreEqual(newName, item.Name);
    }
}
