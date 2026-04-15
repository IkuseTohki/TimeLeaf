using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// ファイルシステムをベースとしたアイデンティティシードリポジトリの実装。
/// </summary>
public class FileIdentitySeedRepository : IIdentitySeedRepository
{
    private const string SeedFileName = "seed.json";
    private readonly string _portableDirectory;
    private readonly string _homeDirectory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private string? _currentFilePath;

    public FileIdentitySeedRepository(string portableDirectory, string homeDirectory)
    {
        _portableDirectory = portableDirectory;
        _homeDirectory = homeDirectory;
    }

    public async Task<IdentitySeedDto?> LoadAsync()
    {
        var portablePath = Path.Combine(_portableDirectory, SeedFileName);
        var homePath = Path.Combine(_homeDirectory, SeedFileName);

        if (File.Exists(portablePath))
        {
            _currentFilePath = portablePath;
        }
        else if (File.Exists(homePath))
        {
            _currentFilePath = homePath;
        }
        else
        {
            // 探索の結果、存在しない場合は作成の準備（ホームパスを選択）
            if (!Directory.Exists(_homeDirectory))
            {
                Directory.CreateDirectory(_homeDirectory);
            }
            _currentFilePath = homePath;
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_currentFilePath);
            return JsonSerializer.Deserialize<IdentitySeedDto>(json, JsonOptions);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task SaveAsync(IdentitySeedDto seed)
    {
        if (_currentFilePath == null)
            throw new InvalidOperationException("Identity file path is not set.");

        var json = JsonSerializer.Serialize(seed, JsonOptions);
        await File.WriteAllTextAsync(_currentFilePath, json);
    }

    public string GetFilePath() => _currentFilePath ?? Path.Combine(_homeDirectory, SeedFileName);
}
