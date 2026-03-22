using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class UserDtoTests
{
    /// <summary>
    /// テスト観点: UserエンティティからUserDtoへ正しく変換できることを確認する。
    /// </summary>
    [TestMethod]
    public void FromEntity_ShouldMapProperties()
    {
        // Arrange
        var user = new User(Guid.NewGuid(), "田中 太郎", "#FF0000", "user1.png");
        var updatedAt = DateTime.UtcNow;

        // Act
        var dto = UserDto.FromEntity(user, updatedAt);

        // Assert
        Assert.AreEqual(user.Id, dto.Id);
        Assert.AreEqual(user.DisplayName, dto.DisplayName);
        Assert.AreEqual(user.ThemeColor, dto.ThemeColor);
        Assert.AreEqual(user.IconPath, dto.IconPath);
        Assert.AreEqual(updatedAt, dto.UpdatedAt);
    }

    /// <summary>
    /// テスト観点: UserDtoからUserエンティティへ正しく復元できることを確認する。
    /// </summary>
    [TestMethod]
    public void ToEntity_ShouldRestoreProperties()
    {
        // Arrange
        var dto = new UserDto
        {
            Id = Guid.NewGuid(),
            DisplayName = "佐藤 次郎",
            ThemeColor = "#00FF00",
            IconPath = "user2.png",
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var user = dto.ToEntity();

        // Assert
        Assert.AreEqual(dto.Id, user.Id);
        Assert.AreEqual(dto.DisplayName, user.DisplayName);
        Assert.AreEqual(dto.ThemeColor, user.ThemeColor);
        Assert.AreEqual(dto.IconPath, user.IconPath);
    }
}
