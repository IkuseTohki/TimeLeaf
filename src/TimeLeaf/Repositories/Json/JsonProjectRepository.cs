using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

#pragma warning disable CS0067
    public event Action<Guid>? ProjectChanged;
#pragma warning restore CS0067

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

    public async System.Threading.Tasks.Task<Project?> LoadAsync(Guid projectId)
    {
        var projects = await LoadAllAsync();
        return projects.FirstOrDefault(p => p.Id == projectId);
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, projects, _options);
    }

    public System.Threading.Tasks.Task SaveAsync(Project project)
    {
        // 単一ファイル版では個別保存は非効率だが、互換性のために実装
        // ここでは全件保存を呼び出す
        return SaveAllAsync(new[] { project });
    }
}
