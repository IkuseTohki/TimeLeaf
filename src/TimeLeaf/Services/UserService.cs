using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services;

/// <summary>
/// IUserService の実装クラス。
/// ユーザー情報のメモリキャッシュ管理と、変更通知を行います。
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ConcurrentDictionary<Guid, User> _cache = new();

    public event Action<User>? UserChanged;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        if (userId == Guid.Empty) return null;

        // キャッシュにあれば即座に返す
        if (_cache.TryGetValue(userId, out var cachedUser))
        {
            return cachedUser;
        }

        // なければリポジトリから取得
        var user = await _userRepository.GetUserAsync(userId);
        if (user != null)
        {
            _cache[userId] = user;
        }

        return user;
    }

    public void UpdateCache(User user)
    {
        if (user == null) return;

        // キャッシュを更新または追加
        _cache[user.Id] = user;

        // 変更を通知
        UserChanged?.Invoke(user);
    }
}
