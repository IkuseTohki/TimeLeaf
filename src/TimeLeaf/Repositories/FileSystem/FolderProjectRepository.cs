using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 仕様(ADR-0001)に基づき、フォルダベースでプロジェクト履歴を管理するリポジトリ。
/// </summary>
public class FolderProjectRepository : IProjectRepository, IDisposable
{
    private readonly string _baseDirectory;
    private readonly ICurrentUserService _userService;
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };

    private readonly ConcurrentDictionary<string, string> _lastSavedContent = new();
    private readonly FileSystemWatcher _watcher;

    public event Action<Guid>? ProjectChanged;

    public FolderProjectRepository(string baseDirectory, ICurrentUserService userService)
    {
        _baseDirectory = baseDirectory;
        _userService = userService;

        if (!Directory.Exists(_baseDirectory))
        {
            Directory.CreateDirectory(_baseDirectory);
        }

        // FileSystemWatcher の初期化
        _watcher = new FileSystemWatcher(_baseDirectory)
        {
            IncludeSubdirectories = true,
            Filter = "*.json",
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
        };
        _watcher.Created += OnFileCreated;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // パス例: storage/{Guid}_{Name}/changes/{Timestamp}_{User}_{Guid}_{Category}.json
        // ルートディレクトリからの相対パスを取得して解析
        var relativePath = Path.GetRelativePath(_baseDirectory, e.FullPath);
        var pathParts = relativePath.Split(Path.DirectorySeparatorChar);

        if (pathParts.Length >= 2)
        {
            // 最初のディレクトリ名が {Guid}_{Name} 形式であることを期待
            var projectDirName = pathParts[0];
            var idPart = projectDirName.Split('_')[0];

            if (Guid.TryParse(idPart, out var projectId))
            {
                System.Threading.Tasks.Task.Run(() => ProjectChanged?.Invoke(projectId));
            }
        }
    }

    /// <summary>
    /// 各プロジェクトフォルダをスキャンし、.project の作成日時に基づいて時系列昇順でプロジェクトをロードします。
    /// </summary>
    public async System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync()
    {
        if (!Directory.Exists(_baseDirectory)) return new List<Project>();

        var projectDirs = Directory.GetDirectories(_baseDirectory);
        var projectTasks = projectDirs.Select(async dir =>
        {
            // .project ファイルが存在する場合のみプロジェクトとして認識
            var metaPath = Path.Combine(dir, ".project");
            if (!File.Exists(metaPath)) return null;

            var metaJson = await File.ReadAllTextAsync(metaPath);
            var meta = JsonSerializer.Deserialize<ProjectMetadataDto>(metaJson, _options);
            if (meta == null) return null;

            // .project から判明している ID を渡して Replay を開始
            var project = await ReplayProjectAsync(dir, meta.ProjectId);
            return project != null ? new { Project = project, CreatedAt = meta.CreatedAt } : null;
        });

        var results = await System.Threading.Tasks.Task.WhenAll(projectTasks);

        return results
            .Where(r => r != null)
            .OrderBy(r => r!.CreatedAt)
            .Select(r => r!.Project)
            .ToList();
    }

    /// <summary>
    /// 特定のプロジェクトのみを再読み込み(Replay)します。
    /// </summary>
    public async System.Threading.Tasks.Task<Project?> LoadAsync(Guid projectId)
    {
        var projectDirs = Directory.GetDirectories(_baseDirectory);
        var targetDir = projectDirs.FirstOrDefault(d => Path.GetFileName(d).StartsWith(projectId.ToString()));

        if (targetDir == null) return null;
        return await ReplayProjectAsync(targetDir, projectId);
    }

    private async Task<Project?> ReplayProjectAsync(string projectDirPath, Guid projectId)
    {
        var changesDir = Path.Combine(projectDirPath, "changes");
        if (!Directory.Exists(changesDir)) return new Project { Id = projectId };

        var files = Directory.GetFiles(changesDir, "*.json")
            .Select(f => new { Path = f, Meta = CommitFileName.Parse(Path.GetFileName(f)) })
            .OrderBy(x => x.Meta.Timestamp)
            .ThenBy(x => x.Meta.Guid)
            .ToList();

        // 履歴がなくても、IDが分かっていればプロジェクトとして成立させる
        var project = new Project { Id = projectId };

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file.Path);
                var cacheKey = GetCacheKey(projectId, file.Meta.Category);
                _lastSavedContent[cacheKey] = json;

                switch (file.Meta.Category)
                {
                    case "ProjectBasic":
                        var basic = JsonSerializer.Deserialize<ProjectBasicDto>(json, _options);
                        if (basic != null) project.Name = basic.Name;
                        break;

                    case "ProjectTasks":
                        var tasks = JsonSerializer.Deserialize<List<TaskDto>>(json, _options);
                        if (tasks != null)
                        {
                            project.Tasks.Clear();
                            foreach (var t in tasks) project.Tasks.Add(new Models.Entities.Task { Id = t.Id, Name = t.Name });
                        }
                        break;
                }
            }
            catch (IOException) {
                /* ファイルが他プロセスで使用中の場合は一旦スキップ（リトライは将来課題） */
            }
        }
        return project;
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        foreach (var project in projects) await SaveAsync(project);
    }

    public async System.Threading.Tasks.Task SaveAsync(Project project)
    {
        var projectDir = Path.Combine(_baseDirectory, $"{project.Id}_{project.Name}");
        if (!Directory.Exists(projectDir)) Directory.CreateDirectory(projectDir);

        var metaFilePath = Path.Combine(projectDir, ".project");
        if (!File.Exists(metaFilePath))
        {
            var metadata = new ProjectMetadataDto(project.Id, DateTime.Now, _userService.GetCurrentUserId(), 1);
            await File.WriteAllTextAsync(metaFilePath, JsonSerializer.Serialize(metadata, _options));
        }

        var changesDir = Path.Combine(projectDir, "changes");

        // ProjectBasic Snapshot (Id を含まない)
        var basicSnapshot = new ProjectBasicDto(project.Name);
        await TrySaveCategoryAsync(project.Id, changesDir, "ProjectBasic", basicSnapshot);

        // ProjectTasks Snapshot (識別のための TaskId は保持)
        var tasksSnapshot = project.Tasks.Select(t => new TaskDto(t.Id, t.Name)).ToList();
        await TrySaveCategoryAsync(project.Id, changesDir, "ProjectTasks", tasksSnapshot);
    }

    private async System.Threading.Tasks.Task TrySaveCategoryAsync(Guid projectId, string changesDir, string category, object data)
    {
        var json = JsonSerializer.Serialize(data, _options);
        var cacheKey = GetCacheKey(projectId, category);

        if (_lastSavedContent.TryGetValue(cacheKey, out var lastJson) && lastJson == json) return;

        if (!Directory.Exists(changesDir)) Directory.CreateDirectory(changesDir);

        var fileName = CommitFileName.Generate(DateTime.Now, _userService.GetCurrentUserId(), Guid.NewGuid(), category);
        await File.WriteAllTextAsync(Path.Combine(changesDir, fileName), json);

        _lastSavedContent[cacheKey] = json;
    }

    private string GetCacheKey(Guid projectId, string category) => $"{projectId}_{category}";

    public void Dispose()
    {
        _watcher.Dispose();
    }

    private record ProjectBasicDto(string Name);
    private record TaskDto(Guid Id, string Name);
    private record ProjectMetadataDto(Guid ProjectId, DateTime CreatedAt, string CreatedBy, int SchemaVersion);
}
