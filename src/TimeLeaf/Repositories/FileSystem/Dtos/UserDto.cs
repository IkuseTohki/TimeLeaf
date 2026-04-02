using System;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// ユーザー情報の永続化用 DTO。
/// </summary>
public class UserDto
{
    /// <summary>
    /// ユーザーID。
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 表示名。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// テーマカラー。
    /// </summary>
    public string ThemeColor { get; set; } = string.Empty;

    /// <summary>
    /// アイコンパス。
    /// </summary>
    public string IconPath { get; set; } = string.Empty;

    /// <summary>
    /// プロフィールの最終更新日時。
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// エンティティから DTO を作成します。
    /// </summary>
    public static UserDto FromEntity(User user, DateTime updatedAt)
    {
        return new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            ThemeColor = user.ThemeColor,
            IconPath = user.IconPath,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// DTO からエンティティを作成します。
    /// </summary>
    public User ToEntity()
    {
        return new User(Id, DisplayName, ThemeColor, IconPath);
    }
}
