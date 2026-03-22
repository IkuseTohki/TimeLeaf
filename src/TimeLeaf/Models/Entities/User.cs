using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// TimeLeaf のユーザー情報を表すエンティティ。
/// </summary>
public class User
{
    /// <summary>
    /// ユーザーの一意な識別子（GUID）。
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// 画面に表示されるユーザー名。
    /// </summary>
    public string DisplayName { get; private set; }

    /// <summary>
    /// ユーザーを識別するためのテーマカラー（16進数カラーコード）。
    /// </summary>
    public string ThemeColor { get; private set; }

    /// <summary>
    /// アイコン画像ファイルへの相対パス。
    /// </summary>
    public string IconPath { get; private set; }

    /// <summary>
    /// User エンティティの新しいインスタンスを初期化します。
    /// </summary>
    /// <param name="id">ユーザーの一意なID。</param>
    /// <param name="displayName">表示名。</param>
    /// <param name="themeColor">テーマカラー。</param>
    /// <param name="iconPath">アイコン画像パス。</param>
    public User(Guid id, string displayName, string themeColor, string iconPath)
    {
        Id = id;
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ThemeColor = themeColor ?? throw new ArgumentNullException(nameof(themeColor));
        IconPath = iconPath ?? throw new ArgumentNullException(nameof(iconPath));
    }

    /// <summary>
    /// プロフィール情報を更新します。
    /// </summary>
    /// <param name="displayName">新しい表示名。</param>
    /// <param name="themeColor">新しいテーマカラー。</param>
    /// <param name="iconPath">新しいアイコン画像パス。</param>
    public void UpdateProfile(string displayName, string themeColor, string iconPath)
    {
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        ThemeColor = themeColor ?? throw new ArgumentNullException(nameof(themeColor));
        IconPath = iconPath ?? throw new ArgumentNullException(nameof(iconPath));
    }
}
