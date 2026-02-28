using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 仕様(ADR-0001)に基づき、フォルダベースでプロジェクト履歴を管理するリポジトリ。
/// </summary>
public class FolderProjectRepository : IProjectRepository, IDisposable
{
    private readonly string _baseDirectory;
    private readonly IProjectStorageMonitor _monitor;
    private readonly IProjectFileSystemSerializer _serializer;
    private readonly ICommitFileNameGenerator _fileNameGenerator;
    private readonly ILogger<FolderProjectRepository> _logger;

    private readonly ConcurrentDictionary<string, string> _lastSavedContent = new();

    public event Action<Guid>? ProjectChanged;

    public FolderProjectRepository(
        string baseDirectory,
        IProjectStorageMonitor monitor,
        IProjectFileSystemSerializer serializer,
        ICommitFileNameGenerator fileNameGenerator,
        ILogger<FolderProjectRepository> logger)
    {
        _baseDirectory = baseDirectory;
        _monitor = monitor;
        _serializer = serializer;
        _fileNameGenerator = fileNameGenerator;
        _logger = logger;

        _logger.LogInformation("FolderProjectRepository initializing with base directory: {BaseDirectory}", _baseDirectory);

        _monitor.ProjectChanged += OnMonitorProjectChanged;
    }

    private void OnMonitorProjectChanged(Guid projectId)
    {
        // UIスレッド等での実行を考慮し、Task.Runで非同期にイベントを発火させる
        System.Threading.Tasks.Task.Run(() => ProjectChanged?.Invoke(projectId));
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
                var meta = _serializer.Deserialize<ProjectMetadataDto>(metaJson);
                if (meta == null)
                {
                    _logger.LogWarning("Could not deserialize .project metadata from {MetaPath}. Skipping.", metaPath);
                    return null;
                }
                _logger.LogDebug("Loading project {ProjectId} from {Directory}", meta.ProjectId, dir);
                // .project から判明している ID を渡して Replay を開始
                var project = await ReplayProjectAsync(dir, meta.ProjectId, meta.CreatedAt);
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

        var metaPath = Path.Combine(targetDir, ".project");
        DateTime createdAt = DateTime.UtcNow;
        if (File.Exists(metaPath))
        {
            var metaJson = await File.ReadAllTextAsync(metaPath);
            var meta = _serializer.Deserialize<ProjectMetadataDto>(metaJson);
            if (meta != null)
            {
                createdAt = meta.CreatedAt;
            }
        }

        return await ReplayProjectAsync(targetDir, projectId, createdAt);
    }

    private async Task<Project?> ReplayProjectAsync(string projectDirPath, Guid projectId, DateTime createdAt)
    {
        _logger.LogInformation("Replaying project history for {ProjectId} from {ProjectDirPath}", projectId, projectDirPath);
        var changesDir = Path.Combine(projectDirPath, "changes");
        if (!Directory.Exists(changesDir))
        {
            _logger.LogWarning("Changes directory {ChangesDir} not found for {ProjectId}. Returning new Project.", changesDir, projectId);
            return new Project { Id = projectId, CreatedAt = createdAt };
        }

        var files = Directory.GetFiles(changesDir, "*.json")
            .Select(f => new { Path = f, Meta = _fileNameGenerator.Parse(Path.GetFileName(f)) })
            .OrderBy(x => x.Meta.Timestamp)
            .ThenBy(x => x.Meta.Guid)
            .ToList();

        _logger.LogDebug("Found {ChangeFileCount} change files for project {ProjectId}.", files.Count, projectId);

        // 履歴がなくても、IDが分かっていればプロジェクトとして成立させる
        var project = new Project { Id = projectId, CreatedAt = createdAt };
        var allCommentData = new List<(CommentDto Dto, DateTime Timestamp)>();

        foreach (var file in files)
        {
            try
            {
                _logger.LogDebug("Processing change file: {FileName} (Category: {Category})", Path.GetFileName(file.Path), file.Meta.Category);
                var json = await File.ReadAllTextAsync(file.Path);
                var cacheKey = GetCacheKey(projectId, file.Meta.Category);

                // コメント以外は最新の状態を保持するためにキャッシュを更新
                if (file.Meta.Category != "Comment")
                {
                    _lastSavedContent[cacheKey] = json;
                }

                switch (file.Meta.Category)
                {
                    case "ProjectBasic":
                        var basic = _serializer.Deserialize<ProjectBasicDto>(json);
                        if (basic != null)
                        {
                            project.UpdateName(basic.Name);
                            project.UpdateDescription(basic.Description);
                            project.UpdateStatus(basic.Status);
                            project.UpdateHealth(basic.HealthStatus);

                            project.ClearMilestones();
                            if (basic.Milestones != null)
                            {
                                foreach (var m in basic.Milestones)
                                {
                                    project.AddMilestone(new Milestone { Date = m.Date, Label = m.Label });
                                }
                            }
                            _logger.LogTrace("Replayed ProjectBasic for {ProjectId}. Name: {Name}", projectId, project.Name);
                        }
                        break;

                    case "ProjectTasks":
                        var tasks = _serializer.Deserialize<List<ProjectTaskDto>>(json);
                        if (tasks != null)
                        {
                            // 既存のコメントを退避（スナップショットにはコメントが含まれないため、Replay済みのものを保持する）
                            var commentMap = project.Tasks.ToDictionary(t => t.Id, t => t.Comments.ToList());

                            project.ClearTasks();
                            foreach (var t in tasks)
                            {
                                var newTask = new ProjectTask(
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
                                    t.Assignee ?? string.Empty,
                                    t.Dependencies ?? new(),
                                    null // Comments will be loaded below
                                );

                                if (commentMap.TryGetValue(newTask.Id, out var existingComments))
                                {
                                    newTask.LoadComments(existingComments);
                                }

                                project.AddTask(newTask);
                            }
                            _logger.LogTrace("Replayed {TaskCount} tasks for ProjectTasks for {ProjectId}.", tasks.Count, projectId);
                        }
                        break;

                    case "Comment":
                        var commentDto = _serializer.Deserialize<CommentDto>(json);
                        if (commentDto != null)
                        {
                            allCommentData.Add((commentDto, file.Meta.Timestamp));
                            _lastSavedContent[GetCacheKey(projectId, $"Comment_{commentDto.Id}")] = json;
                        }
                        break;
                }
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, "IOException while processing file {FileName} for project {ProjectId}. Skipping.", Path.GetFileName(file.Path), projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing file {FileName} for project {ProjectId}.", Path.GetFileName(file.Path), projectId);
            }
        }

        // コメントをタスクに紐付け
        foreach (var entry in allCommentData)
        {
            var dto = entry.Dto;
            var targetTask = project.Tasks.FirstOrDefault(t => t.Id == dto.TaskId);
            if (targetTask != null)
            {
                if (!targetTask.Comments.Any(c => c.Id == dto.Id))
                {
                    targetTask.AddComment(new Comment
                    {
                        Id = dto.Id,
                        TaskId = dto.TaskId,
                        AuthorId = dto.AuthorId,
                        CreatedAt = entry.Timestamp,
                        Content = dto.Content,
                        AttachmentLinks = dto.AttachmentLinks ?? new()
                    });
                }
            }
        }

        if (files.Any())
        {
            var latestTimestamp = files.Last().Meta.Timestamp;
            project.SetUpdatedAt(latestTimestamp);
        }

        return project;
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects, string userId)
    {
        foreach (var project in projects) await SaveAsync(project, userId);
    }

    public async System.Threading.Tasks.Task SaveAsync(Project project, string userId)
    {
        _logger.LogInformation("Saving project: {ProjectName} ({ProjectId})", project.Name, project.Id);
        try
        {
            var projectDir = Path.Combine(_baseDirectory, $"{project.Id}_{project.Name}");
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            var metaFilePath = Path.Combine(projectDir, ".project");
            if (!File.Exists(metaFilePath))
            {
                var metadata = new ProjectMetadataDto(project.Id, project.CreatedAt, userId, 1);
                await File.WriteAllTextAsync(metaFilePath, _serializer.Serialize(metadata));
            }

            var changesDir = Path.Combine(projectDir, "changes");
            var commitTime = DateTime.UtcNow;

            // 1. ProjectBasic Snapshot
            var basicSnapshot = new ProjectBasicDto(
                project.Name,
                project.Description,
                project.Status,
                project.HealthStatus,
                project.Milestones.Select(m => new MilestoneDto(m.Date, m.Label)).ToList());
            await TrySaveCategoryAsync(project.Id, changesDir, "ProjectBasic", basicSnapshot, commitTime, userId);

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
            await TrySaveCategoryAsync(project.Id, changesDir, "ProjectTasks", tasksSnapshot, commitTime, userId);

            // 3. Comments (Incremental)
            foreach (var t in project.Tasks)
            {
                foreach (var c in t.Comments)
                {
                    await TrySaveCommentAsync(project.Id, changesDir, c, userId);
                }
            }

            project.SetUpdatedAt(commitTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project {ProjectName} ({ProjectId})", project.Name, project.Id);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TrySaveCommentAsync(Guid projectId, string changesDir, Comment comment, string userId)
    {
        var category = "Comment";
        var cacheKey = GetCacheKey(projectId, $"{category}_{comment.Id}");

        if (_lastSavedContent.ContainsKey(cacheKey)) return;

        if (!Directory.Exists(changesDir)) Directory.CreateDirectory(changesDir);

        var commentDto = new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.AttachmentLinks);
        var json = _serializer.Serialize(commentDto);
        var fileName = _fileNameGenerator.Generate(comment.CreatedAt, userId, comment.Id, category);
        var fullPath = Path.Combine(changesDir, fileName);

        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            _lastSavedContent[cacheKey] = json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving incremental comment {CommentId} to {FileName}.", comment.Id, fileName);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TrySaveCategoryAsync(Guid projectId, string changesDir, string category, object data, DateTime timestamp, string userId)
    {
        var json = _serializer.Serialize(data);
        var cacheKey = GetCacheKey(projectId, category);

        if (_lastSavedContent.TryGetValue(cacheKey, out var lastJson) && lastJson == json) return;

        if (!Directory.Exists(changesDir)) Directory.CreateDirectory(changesDir);

        var fileName = _fileNameGenerator.Generate(timestamp, userId, Guid.NewGuid(), category);
        var fullPath = Path.Combine(changesDir, fileName);
        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            _lastSavedContent[cacheKey] = json;
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
    }
}
