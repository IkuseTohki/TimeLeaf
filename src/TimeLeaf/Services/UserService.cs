using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services;

/// <summary>
/// IUserService の実装クラス。
/// ユーザー情報のメモリキャッシュ管理と、変更通知を行います。
/// </summary>
public class UserService : IUserService, IDisposable
{
    private readonly IUserRepository _userRepository;
    private readonly ConcurrentDictionary<Guid, User> _cache = new();

    public event Action<User>? UserChanged;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));

        // リポジトリの変更通知を購読
        _userRepository.UserChanged += OnRepositoryUserChanged;
    }

    /// <inheritdoc />
    public async Task PreloadAsync()
    {
        var users = await _userRepository.GetAllUsersAsync();
        foreach (var user in users)
        {
            _cache[user.Id] = user;
        }
    }

    private async void OnRepositoryUserChanged(object? sender, Guid userId)
    {
        // ファイルが更新されたら再取得してキャッシュを更新
        var updatedUser = await _userRepository.GetUserAsync(userId);
        if (updatedUser != null)
        {
            UpdateCache(updatedUser);
        }
    }

    public async Task<User?> GetUserAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return null;

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

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        var allUsers = await _userRepository.GetAllUsersAsync();
        return allUsers.Where(u => !u.IsDeleted);
    }

    /// <inheritdoc />
    public async Task DeleteUserAsync(Guid userId)
    {
        var user = await GetUserAsync(userId);
        if (user != null)
        {
            user.MarkAsDeleted();
            await _userRepository.SaveUserAsync(user);
        }
    }

    public User? GetCachedUser(Guid userId)
    {
        _cache.TryGetValue(userId, out var user);
        return user;
    }

    public string GetUserName(string userIdString)
    {
        if (string.IsNullOrEmpty(userIdString))
            return "Unassigned";
        if (!Guid.TryParse(userIdString, out var guid))
            return userIdString;

        if (_cache.TryGetValue(guid, out var user))
        {
            return user.DisplayName;
        }

        return "Unknown";
    }

    public string? GetUserIdByName(string name)
    {
        var user = _cache.Values.FirstOrDefault(u => u.DisplayName == name);
        return user?.Id.ToString();
    }

    public void UpdateCache(User user)
    {
        if (user == null)
            return;

        // キャッシュを更新または追加
        _cache[user.Id] = user;

        // 変更を通知
        UserChanged?.Invoke(user);
    }

    public void Dispose()
    {
        _userRepository.UserChanged -= OnRepositoryUserChanged;
    }
}
