using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// ファイルシステムを用いてユーザー情報を永続化するリポジトリの実装。
/// </summary>
public class FileSystemUserRepository : IUserRepository
{
    private readonly string _usersDirectory;
    private readonly FileSystemWatcher _watcher;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 日本語をエスケープせずに保存
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// ユーザープロフィールが変更されたときに発生します。
    /// </summary>
    public event EventHandler<Guid>? UserChanged;

    /// <summary>
    /// 指定されたディレクトリを使用してリポジトリを初期化します。
    /// </summary>
    /// <param name="usersDirectory">ユーザー情報を格納するディレクトリのパス。</param>
    public FileSystemUserRepository(string usersDirectory)
    {
        _usersDirectory = usersDirectory;
        if (!Directory.Exists(_usersDirectory))
        {
            Directory.CreateDirectory(_usersDirectory);
        }

        // フォルダ監視の初期化
        _watcher = new FileSystemWatcher(_usersDirectory, "*.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileNameWithoutExtension(e.Name);
        if (Guid.TryParse(fileName, out var userId))
        {
            UserChanged?.Invoke(this, userId);
        }
    }

    /// <inheritdoc />
    public async Task<User?> GetUserAsync(Guid userId)
    {
        var filePath = GetFilePath(userId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        // ファイルが他プロセスによって書き込み中の可能性があるため、
        // 読み取り共有モードで開き、必要に応じてリトライを行うなどの堅牢性が望ましいが、
        // まずは単純な実装とする。
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var dto = await JsonSerializer.DeserializeAsync<UserDto>(stream, JsonOptions);
            return dto?.ToEntity();
        }
        catch (IOException)
        {
            // 書き込み中などの一時的なエラー
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var users = new List<User>();
        var files = Directory.GetFiles(_usersDirectory, "*.json");

        foreach (var file in files)
        {
            try
            {
                using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                var dto = await JsonSerializer.DeserializeAsync<UserDto>(stream, JsonOptions);
                if (dto != null)
                {
                    users.Add(dto.ToEntity());
                }
            }
            catch
            {
                // 不正なJSONファイルやロック中のファイルはスキップ
                continue;
            }
        }

        return users;
    }

    /// <inheritdoc />
    public async Task SaveUserAsync(User user)
    {
        var filePath = GetFilePath(user.Id);
        var dto = UserDto.FromEntity(user, DateTime.Now);

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private string GetFilePath(Guid userId)
    {
        return Path.Combine(_usersDirectory, $"{userId}.json");
    }

    /// <summary>
    /// リソースを破棄します。
    /// </summary>
    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Changed -= OnFileChanged;
        _watcher.Created -= OnFileChanged;
        _watcher.Dispose();
    }
}
