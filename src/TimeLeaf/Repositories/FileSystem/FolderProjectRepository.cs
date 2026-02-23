using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 仕様(ADR-0001)に基づき、フォルダベースでプロジェクト履歴を管理するリポジトリ。
/// </summary>
public class FolderProjectRepository : IProjectRepository, IDisposable
{
    private readonly string _baseDirectory;
    private readonly ICurrentUserService _userService;
    private readonly ILogger<FolderProjectRepository> _logger;
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() } // Enum を文字列で保存
    };

    private readonly ConcurrentDictionary<string, string> _lastSavedContent = new();
    private readonly ConcurrentDictionary<string, byte> _justWrittenFiles = new();
    private readonly FileSystemWatcher _watcher;

    public event Action<Guid>? ProjectChanged;

    public FolderProjectRepository(string baseDirectory, ICurrentUserService userService, ILogger<FolderProjectRepository> logger)
    {
        _baseDirectory = baseDirectory;
        _userService = userService;
        _logger = logger;

        _logger.LogInformation("FolderProjectRepository initializing with base directory: {BaseDirectory}", _baseDirectory);

        if (!Directory.Exists(_baseDirectory))
        {
            _logger.LogInformation("Base directory {BaseDirectory} does not exist. Creating it.", _baseDirectory);
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
        _watcher.Changed += OnFileCreated;
        _watcher.EnableRaisingEvents = true;
        _logger.LogDebug("FileSystemWatcher initialized for {BaseDirectory}", _baseDirectory);
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = Path.GetFileName(e.FullPath);
        if (_justWrittenFiles.TryRemove(fileName, out _))
        {
            _logger.LogDebug("Ignoring file change event for our own write: {FileName}", fileName);
            return;
        }

        _logger.LogInformation("File created/changed event detected: {FullPath} (ChangeType: {ChangeType})", e.FullPath, e.ChangeType);
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
                _logger.LogDebug("Project ID {ProjectId} extracted from path. Invoking ProjectChanged event.", projectId);
                System.Threading.Tasks.Task.Run(() => ProjectChanged?.Invoke(projectId));
            }
            else
            {
                _logger.LogWarning("Could not parse Project ID from directory name: {ProjectDirName}", projectDirName);
            }
        }
        else
        {
            _logger.LogWarning("Invalid file path format for project change detection: {FullPath}", e.FullPath);
        }
    }

    /// <summary>
    /// 各プロジェクトフォルダをスキャンし、.project の作成日時に基づいて時系列昇順でプロジェクトをロードします。
    /// </summary>
    public async System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync()
    {
        _logger.LogInformation("Loading all projects from {BaseDirectory}", _baseDirectory);
        if (!Directory.Exists(_baseDirectory))
        {
            _logger.LogWarning("Base directory {BaseDirectory} does not exist. Returning empty list.", _baseDirectory);
            return new List<Project>();
        }

        var projectDirs = Directory.GetDirectories(_baseDirectory);
        _logger.LogDebug("Found {ProjectDirCount} project directories.", projectDirs.Length);

        var projectTasks = projectDirs.Select(async dir =>
        {
            // .project ファイルが存在する場合のみプロジェクトとして認識
            var metaPath = Path.Combine(dir, ".project");
            if (!File.Exists(metaPath))
            {
                _logger.LogDebug(".project file not found in {Directory}. Skipping.", dir);
                return null;
            }

            try
            {
                var metaJson = await File.ReadAllTextAsync(metaPath);
                var meta = JsonSerializer.Deserialize<ProjectMetadataDto>(metaJson, _options);
                if (meta == null)
                {
                    _logger.LogWarning("Could not deserialize .project metadata from {MetaPath}. Skipping.", metaPath);
                    return null;
                }
                _logger.LogDebug("Loading project {ProjectId} from {Directory}", meta.ProjectId, dir);
                // .project から判明している ID を渡して Replay を開始
                var project = await ReplayProjectAsync(dir, meta.ProjectId);
                return project != null ? new { Project = project, CreatedAt = meta.CreatedAt } : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project metadata from {MetaPath}. Skipping.", metaPath);
                return null;
            }
        });

        var results = await System.Threading.Tasks.Task.WhenAll(projectTasks);
        _logger.LogInformation("Finished loading all projects. Found {LoadedProjectCount} valid projects.", results.Count(r => r != null));

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
        _logger.LogInformation("Loading single project: {ProjectId}", projectId);
        var projectDirs = Directory.GetDirectories(_baseDirectory);
        var targetDir = projectDirs.FirstOrDefault(d => Path.GetFileName(d).StartsWith(projectId.ToString()));

        if (targetDir == null)
        {
            _logger.LogWarning("Project directory for {ProjectId} not found.", projectId);
            return null;
        }
        _logger.LogDebug("Found project directory {TargetDir} for {ProjectId}.", targetDir, projectId);
        return await ReplayProjectAsync(targetDir, projectId);
    }

    private async Task<Project?> ReplayProjectAsync(string projectDirPath, Guid projectId)
    {
        _logger.LogInformation("Replaying project history for {ProjectId} from {ProjectDirPath}", projectId, projectDirPath);
        var changesDir = Path.Combine(projectDirPath, "changes");
        if (!Directory.Exists(changesDir))
        {
            _logger.LogWarning("Changes directory {ChangesDir} not found for {ProjectId}. Returning new Project.", changesDir, projectId);
            return new Project { Id = projectId };
        }

        var files = Directory.GetFiles(changesDir, "*.json")
            .Select(f => new { Path = f, Meta = CommitFileName.Parse(Path.GetFileName(f)) })
            .OrderBy(x => x.Meta.Timestamp)
            .ThenBy(x => x.Meta.Guid)
            .ToList();

        _logger.LogDebug("Found {ChangeFileCount} change files for project {ProjectId}.", files.Count, projectId);

        // 履歴がなくても、IDが分かっていればプロジェクトとして成立させる
        var project = new Project { Id = projectId };

        foreach (var file in files)
        {
            try
            {
                _logger.LogDebug("Processing change file: {FileName} (Category: {Category})", Path.GetFileName(file.Path), file.Meta.Category);
                var json = await File.ReadAllTextAsync(file.Path);
                var cacheKey = GetCacheKey(projectId, file.Meta.Category);
                _lastSavedContent[cacheKey] = json;

                switch (file.Meta.Category)
                {
                    case "ProjectBasic":
                        var basic = JsonSerializer.Deserialize<ProjectBasicDto>(json, _options);
                        if (basic != null)
                        {
                            project.Name = basic.Name;
                            project.Description = basic.Description;
                            project.Status = basic.Status;
                            project.HealthStatus = basic.HealthStatus;
                            project.Milestones.Clear();
                            if (basic.Milestones != null)
                            {
                                foreach (var m in basic.Milestones)
                                {
                                    project.Milestones.Add(new Milestone { Date = m.Date, Label = m.Label });
                                }
                            }
                            _logger.LogTrace("Replayed ProjectBasic for {ProjectId}. Name: {Name}", projectId, project.Name);
                        }
                        else
                        {
                            _logger.LogWarning("Could not deserialize ProjectBasic from {FileName}", Path.GetFileName(file.Path));
                        }
                        break;

                    case "ProjectTasks":
                        var tasks = JsonSerializer.Deserialize<List<ProjectTaskDto>>(json, _options);
                        if (tasks != null)
                        {
                            project.Tasks.Clear();
                            foreach (var t in tasks)
                            {
                                project.Tasks.Add(new ProjectTask
                                {
                                    Id = t.Id,
                                    Name = t.Name,
                                    Description = t.Description,
                                    Status = t.Status,
                                    Priority = t.Priority,
                                    ScheduledStartDate = t.ScheduledStartDate,
                                    Deadline = t.Deadline,
                                    ActualStartDate = t.ActualStartDate,
                                    ActualEndDate = t.ActualEndDate,
                                    EstimatedCost = t.EstimatedCost,
                                    ActualCost = t.ActualCost,
                                    Assignee = t.Assignee ?? string.Empty,
                                    Dependencies = t.Dependencies ?? new()
                                });
                            }
                            _logger.LogTrace("Replayed {TaskCount} tasks for ProjectTasks for {ProjectId}.", tasks.Count, projectId);
                        }
                        else
                        {
                            _logger.LogWarning("Could not deserialize ProjectTasks from {FileName}", Path.GetFileName(file.Path));
                        }
                        break;
                }
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, "IOException while processing file {FileName} for project {ProjectId}. Skipping.", Path.GetFileName(file.Path), projectId);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JsonException while deserializing file {FileName} for project {ProjectId}. Content: {JsonContent}", Path.GetFileName(file.Path), projectId, await File.ReadAllTextAsync(file.Path));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while processing file {FileName} for project {ProjectId}.", Path.GetFileName(file.Path), projectId);
            }
        }
        return project;
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        _logger.LogInformation("Saving {ProjectCount} projects.", projects.Count());
        foreach (var project in projects) await SaveAsync(project);
    }

    public async System.Threading.Tasks.Task SaveAsync(Project project)
    {
        _logger.LogInformation("Saving project: {ProjectName} ({ProjectId})", project.Name, project.Id);
        try
        {
            var projectDir = Path.Combine(_baseDirectory, $"{project.Id}_{project.Name}");
            if (!Directory.Exists(projectDir))
            {
                _logger.LogDebug("Project directory {ProjectDir} does not exist. Creating it.", projectDir);
                Directory.CreateDirectory(projectDir);
            }

            var metaFilePath = Path.Combine(projectDir, ".project");
            if (!File.Exists(metaFilePath))
            {
                var metadata = new ProjectMetadataDto(project.Id, DateTime.Now, _userService.GetCurrentUserId(), 1);
                await File.WriteAllTextAsync(metaFilePath, JsonSerializer.Serialize(metadata, _options));
                _logger.LogDebug(".project metadata created for {ProjectId}.", project.Id);
            }

            var changesDir = Path.Combine(projectDir, "changes");

            // 1. ProjectBasic Snapshot
            var basicSnapshot = new ProjectBasicDto(
                project.Name,
                project.Description,
                project.Status,
                project.HealthStatus,
                project.Milestones.Select(m => new MilestoneDto(m.Date, m.Label)).ToList());
            await TrySaveCategoryAsync(project.Id, changesDir, "ProjectBasic", basicSnapshot);

            // 2. ProjectTasks Snapshot
            var tasksSnapshot = project.Tasks.Select(t => new ProjectTaskDto(
                t.Id,
                t.Name,
                t.Description,
                t.Status,
                t.Priority,
                t.ScheduledStartDate,
                t.Deadline,
                t.ActualStartDate,
                t.ActualEndDate,
                t.EstimatedCost,
                t.ActualCost,
                t.Assignee,
                t.Dependencies)).ToList();
            await TrySaveCategoryAsync(project.Id, changesDir, "ProjectTasks", tasksSnapshot);
            _logger.LogInformation("Project {ProjectId} saved successfully.", project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project {ProjectName} ({ProjectId})", project.Name, project.Id);
            throw; // 再スローして上位で捕捉されるようにする
        }
    }

    private async System.Threading.Tasks.Task TrySaveCategoryAsync(Guid projectId, string changesDir, string category, object data)
    {
        _logger.LogDebug("Attempting to save category {Category} for project {ProjectId}.", category, projectId);
        var json = JsonSerializer.Serialize(data, _options);
        var cacheKey = GetCacheKey(projectId, category);

        if (_lastSavedContent.TryGetValue(cacheKey, out var lastJson) && lastJson == json)
        {
            _logger.LogTrace("Category {Category} for project {ProjectId} has no changes. Skipping save.", category, projectId);
            return;
        }

        if (!Directory.Exists(changesDir))
        {
            _logger.LogDebug("Changes directory {ChangesDir} does not exist. Creating it.", changesDir);
            Directory.CreateDirectory(changesDir);
        }

        var fileName = CommitFileName.Generate(DateTime.Now, _userService.GetCurrentUserId(), Guid.NewGuid(), category);
        var fullPath = Path.Combine(changesDir, fileName);
        _justWrittenFiles.TryAdd(fileName, 0); // 自前での書き込みであることをマーク

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            _lastSavedContent[cacheKey] = json;
            _logger.LogDebug("Category {Category} for project {ProjectId} saved to {FileName}.", category, projectId, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving category {Category} for project {ProjectId} to {FileName}.", category, projectId, fileName);
            throw;
        }
    }

    private string GetCacheKey(Guid projectId, string category) => $"{projectId}_{category}";

    public void Dispose()
    {
        _logger.LogInformation("FolderProjectRepository disposing.");
        _watcher.Dispose();
    }

    private record MilestoneDto(DateTime Date, string Label);
    private record ProjectBasicDto(string Name, string Description, ProjectStatus Status, ProjectHealth HealthStatus, List<MilestoneDto> Milestones);
    private record ProjectTaskDto(
        Guid Id,
        string Name,
        string Description,
        TimeLeaf.Models.Enums.TaskStatus Status,
        TaskPriority Priority,
        DateTime? ScheduledStartDate,
        DateTime? Deadline,
        DateTime? ActualStartDate,
        DateTime? ActualEndDate,
        double EstimatedCost,
        double ActualCost,
        string Assignee,
        List<Guid> Dependencies);
    private record ProjectMetadataDto(Guid ProjectId, DateTime CreatedAt, string CreatedBy, int SchemaVersion);
}
