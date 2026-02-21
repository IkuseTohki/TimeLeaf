using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.Repositories.Json;

/// <summary>
/// プロジェクトデータを単一のJSONファイルで管理するリポジトリ。
/// </summary>
public class JsonProjectRepository : IProjectRepository
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public JsonProjectRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new List<Project>();
        }

        using var stream = File.OpenRead(_filePath);
        var projects = await JsonSerializer.DeserializeAsync<List<Project>>(stream, _options);
        return projects ?? new List<Project>();
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, projects, _options);
    }
}
