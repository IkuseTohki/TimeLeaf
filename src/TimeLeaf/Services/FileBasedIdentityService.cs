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
    private readonly IUserRepository _userRepository;
    private User? _currentIdentity;

    public Guid CurrentUserId => _currentIdentity?.Id ?? Guid.Empty;

    public FileBasedIdentityService(IIdentitySeedRepository repository, IUserRepository userRepository)
    {
        _repository = repository;
        _userRepository = userRepository;
    }

    public async Task<User> GetCurrentIdentityAsync()
    {
        if (_currentIdentity != null) return _currentIdentity;

        var seed = await _repository.LoadAsync();

        if (seed != null)
        {
            // IDがある場合はプロフィールをロード
            _currentIdentity = await _userRepository.GetUserAsync(seed.Id);

            // プロフィールがまだ作成されていない場合は初期状態で作成
            if (_currentIdentity == null)
            {
                _currentIdentity = new User(seed.Id, Environment.UserName, "#2D5A27", "");
                await _userRepository.SaveUserAsync(_currentIdentity);
            }
        }
        else
        {
            // ID自体がない場合は新規作成
            var newId = Guid.NewGuid();
            _currentIdentity = new User(newId, Environment.UserName, "#2D5A27", "");

            // Seedとプロフィールの両方を保存
            await _repository.SaveAsync(new IdentitySeedDto { Id = newId });
            await _userRepository.SaveUserAsync(_currentIdentity);
        }
        return _currentIdentity;
    }

    public async Task UpdateIdentityAsync(string displayName, string themeColor, string iconPath)
    {
        var identity = await GetCurrentIdentityAsync();
        identity.UpdateProfile(displayName, themeColor, iconPath);

        // 変更を永続化
        await _userRepository.SaveUserAsync(identity);
    }

    public string? GetIdentityFilePath() => _repository.GetFilePath();
}

