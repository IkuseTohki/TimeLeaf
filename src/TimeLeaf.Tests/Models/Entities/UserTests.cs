using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class UserTests
{
    /// <summary>
    /// テスト観点: Userエンティティが正しく初期化され、各プロパティを保持できることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var id = Guid.NewGuid();
        var displayName = "田中 太郎";
        var themeColor = "#FF0000";
        var iconPath = "user1.png";
        var isDeleted = false;

        // Act
        var user = new User(id, displayName, themeColor, iconPath, isDeleted);

        // Assert
        Assert.AreEqual(id, user.Id);
        Assert.AreEqual(displayName, user.DisplayName);
        Assert.AreEqual(themeColor, user.ThemeColor);
        Assert.AreEqual(iconPath, user.IconPath);
        Assert.AreEqual(isDeleted, user.IsDeleted);
    }

    /// <summary>
    /// テスト観点: MarkAsDeleted メソッドによって論理削除フラグが true になることを確認する。
    /// </summary>
    [TestMethod]
    public void MarkAsDeleted_ShouldSetIsDeletedToTrue()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "テストユーザー", "#000000", "", false);

        // Act
        user.MarkAsDeleted();

        // Assert
        Assert.IsTrue(user.IsDeleted);
    }

    /// <summary>
    /// テスト観点: Restore メソッドによって論理削除フラグが false に戻ることを確認する。
    /// </summary>
    [TestMethod]
    public void Restore_ShouldSetIsDeletedToFalse()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "テストユーザー", "#000000", "", true);

        // Act
        user.Restore();

        // Assert
        Assert.IsFalse(user.IsDeleted);
    }
}
