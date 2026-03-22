using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーション利用者のアイデンティティ（自分自身）を管理するサービスのインターフェース。
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// 現在のユーザーIDを取得します。初期化されていない場合は Guid.Empty を返します。
    /// </summary>
    Guid CurrentUserId { get; }

    /// <summary>
    /// 現在のユーザー（自分）の情報を取得します。
    /// 未作成の場合は初期化（新規生成）を行います。
    /// </summary>
    /// <returns>自分自身のユーザーエンティティ。</returns>
    Task<User> GetCurrentIdentityAsync();

    /// <summary>
    /// 自分のプロフィール（表示名やアイコン等）を更新し、永続化します。
    /// </summary>
    /// <param name="displayName">表示名。</param>
    /// <param name="themeColor">テーマカラー。</param>
    /// <param name="iconPath">アイコンパス。</param>
    /// <returns>非同期タスク。</returns>
    Task UpdateIdentityAsync(string displayName, string themeColor, string iconPath);

    /// <summary>
    /// 現在使用しているアイデンティティファイルの物理パスを取得します。
    /// </summary>
    string? GetIdentityFilePath();
}
