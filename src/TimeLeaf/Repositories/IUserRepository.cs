using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Repositories;

/// <summary>
/// ユーザー情報の永続化を担当するリポジトリのインターフェース。
/// </summary>
public interface IUserRepository : IDisposable
{
    /// <summary>
    /// ユーザープロフィールが変更されたときに発生します。
    /// </summary>
    event EventHandler<Guid> UserChanged;

    /// <summary>
    /// ユーザープロフィールを取得します。
    /// </summary>
    /// <param name="userId">ユーザーID。</param>
    /// <returns>ユーザーエンティティ。見つからない場合は null。</returns>
    Task<User?> GetUserAsync(Guid userId);

    /// <summary>
    /// すべての既知のユーザープロフィールを取得します。
    /// </summary>
    /// <returns>ユーザーエンティティのリスト。</returns>
    Task<IEnumerable<User>> GetAllUsersAsync();

    /// <summary>
    /// ユーザープロフィールを保存します。
    /// </summary>
    /// <param name="user">保存するユーザーエンティティ。</param>
    /// <returns>非同期タスク。</returns>
    Task SaveUserAsync(User user);
}
