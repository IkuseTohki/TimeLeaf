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
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 日本語をエスケープせずに保存
        Converters = { new JsonStringEnumConverter() }
    };

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
    }

    /// <inheritdoc />
    public async Task<User?> GetUserAsync(Guid userId)
    {
        var filePath = GetFilePath(userId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        var dto = await JsonSerializer.DeserializeAsync<UserDto>(stream, JsonOptions);
        return dto?.ToEntity();
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
                using var stream = File.OpenRead(file);
                var dto = await JsonSerializer.DeserializeAsync<UserDto>(stream, JsonOptions);
                if (dto != null)
                {
                    users.Add(dto.ToEntity());
                }
            }
            catch
            {
                // 不正なJSONファイルはスキップ
                continue;
            }
        }

        return users;
    }

    /// <inheritdoc />
    public async Task SaveUserAsync(User user)
    {
        var filePath = GetFilePath(user.Id);
        var dto = UserDto.FromEntity(user, DateTime.UtcNow);

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }

    private string GetFilePath(Guid userId)
    {
        return Path.Combine(_usersDirectory, $"{userId}.json");
    }
}
