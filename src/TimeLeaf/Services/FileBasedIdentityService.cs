using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Services;

/// <summary>
/// ファイルシステムをベースとしたアイデンティティ管理サービスの実装。
/// アプリ実行ディレクトリ（ポータブル優先）とホームディレクトリの両方を探索します。
/// </summary>
public class FileBasedIdentityService : IIdentityService
{
    private readonly IIdentitySeedRepository _repository;
    private User? _currentIdentity;

    public Guid CurrentUserId => _currentIdentity?.Id ?? Guid.Empty;

    public FileBasedIdentityService(IIdentitySeedRepository repository)
    {
        _repository = repository;
    }

    public async Task<User> GetCurrentIdentityAsync()
    {
        if (_currentIdentity != null) return _currentIdentity;

        var seed = await _repository.LoadAsync();

        if (seed != null)
        {
            _currentIdentity = new User(seed.Id, Environment.UserName, "#2D5A27", "");
        }
        else
        {
            // 新規作成
            var newId = Guid.NewGuid();
            _currentIdentity = new User(newId, Environment.UserName, "#2D5A27", "");
            await _repository.SaveAsync(new IdentitySeedDto { Id = newId });
        }
        return _currentIdentity;
    }

    public async Task UpdateIdentityAsync(string displayName, string themeColor, string iconPath)
    {
        var identity = await GetCurrentIdentityAsync();
        identity.UpdateProfile(displayName, themeColor, iconPath);
    }

    public string? GetIdentityFilePath() => _repository.GetFilePath();
}

