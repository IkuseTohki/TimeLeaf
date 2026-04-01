using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーション全体のユーザー情報を管理するサービスのインターフェース。
/// キャッシュ保持と変更通知の責務を持ちます。
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 指定されたIDのユーザー情報を取得します。
    /// キャッシュにあればそれを返し、なければリポジトリから取得してキャッシュします。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <returns>ユーザー情報。存在しない場合は null。</returns>
    Task<User?> GetUserAsync(Guid userId);

    /// <summary>
    /// キャッシュを強制的に更新し、変更通知イベントを発行します。
    /// </summary>
    /// <param name="user">最新のユーザー情報。</param>
    void UpdateCache(User user);

    /// <summary>
    /// ユーザー情報が更新された際に発生するイベント。
    /// </summary>
    event Action<User>? UserChanged;
}
