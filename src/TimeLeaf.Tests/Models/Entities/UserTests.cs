using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        // Act
        var user = new User(id, displayName, themeColor, iconPath);

        // Assert
        Assert.AreEqual(id, user.Id);
        Assert.AreEqual(displayName, user.DisplayName);
        Assert.AreEqual(themeColor, user.ThemeColor);
        Assert.AreEqual(iconPath, user.IconPath);
    }
}
