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
        var changesDir = Path.Combine(projectDirPath, "changes");
        if (!Directory.Exists(changesDir))
        {
            _logger.LogWarning("Changes directory {ChangesDir} not found for {ProjectId}. Returning new Project.", changesDir, projectId);
            return new Project { Id = projectId, CreatedAt = createdAt };
        }

        var replayer = new ProjectHistoryReplayer(_serializer, _fileNameGenerator, _logger, _lastSavedContent);
        return await replayer.ReplayAsync(changesDir, projectId, createdAt);
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

            // 1. プロジェクト情報の保存 (3カテゴリ)
            var basicSnapshot = new ProjectBasicDto(project.Name, project.Status, project.HealthStatus);
            await TrySaveCategoryAsync(project.Id, changesDir, "Project_Basic", basicSnapshot, commitTime, userId);

            var descSnapshot = new ProjectDescriptionDto(project.Description);
            await TrySaveCategoryAsync(project.Id, changesDir, "Project_Description", descSnapshot, commitTime, userId);

            var milestoneSnapshot = new ProjectMilestonesDto(project.Milestones.Select(m => new MilestoneDto(m.Date, m.Label)).ToList());
            await TrySaveCategoryAsync(project.Id, changesDir, "Project_Milestones", milestoneSnapshot, commitTime, userId);

            var membersSnapshot = new ProjectMembersDto { AssignedUserIds = project.AssignedUserIds.ToList() };
            await TrySaveCategoryAsync(project.Id, changesDir, "Project_Members", membersSnapshot, commitTime, userId);

            // 2. タスク情報の保存 (各タスク 3カテゴリ)
            foreach (var task in project.Tasks)
            {
                var planning = new TaskPlanningDto(
                    task.Id,
                    task.Name,
                    task.Priority,
                    task.ScheduledStartDate,
                    task.Deadline,
                    task.EstimatedCost,
                    task.Assignee,
                    task.Dependencies.ToList());
                await TrySaveCategoryAsync(task.Id, changesDir, "Task_Planning", planning, commitTime, userId, isTask: true);

                var progress = new TaskProgressDto(
                    task.Id,
                    task.Status,
                    task.ActualStartDate,
                    task.ActualEndDate,
                    task.ActualCost);
                await TrySaveCategoryAsync(task.Id, changesDir, "Task_Progress", progress, commitTime, userId, isTask: true);

                var taskDesc = new TaskDescriptionDto(task.Id, task.Description);
                await TrySaveCategoryAsync(task.Id, changesDir, "Task_Description", taskDesc, commitTime, userId, isTask: true);

                // 3. コメントの保存 (各コメント 1ファイル)
                foreach (var comment in task.Comments)
                {
                    await TrySaveCommentAsync(project.Id, task.Id, changesDir, comment, userId);
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

    private async System.Threading.Tasks.Task TrySaveCommentAsync(Guid projectId, Guid taskId, string changesDir, Comment comment, string userId)
    {
        var category = "Comment";
        var cacheKey = GetCacheKey(taskId, $"{category}_{comment.Id}");

        if (_lastSavedContent.ContainsKey(cacheKey)) return;

        // タスクIDごとのサブフォルダを作成
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        if (!Directory.Exists(taskDir)) Directory.CreateDirectory(taskDir);

        var commentDto = new CommentDto(comment.Id, comment.TaskId, comment.AuthorId, comment.Content, comment.AttachmentLinks);
        var json = _serializer.Serialize(commentDto);
        var fileName = _fileNameGenerator.Generate(comment.CreatedAt, userId, category);
        var fullPath = Path.Combine(taskDir, fileName);

        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.ReadOnly);
            _lastSavedContent[cacheKey] = json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving incremental comment {CommentId} to {FileName}.", comment.Id, fileName);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TrySaveCategoryAsync(Guid entityId, string changesDir, string category, object data, DateTime timestamp, string userId, bool isTask = false)
    {
        var json = _serializer.Serialize(data);
        var cacheKey = GetCacheKey(entityId, category);

        if (_lastSavedContent.TryGetValue(cacheKey, out var lastJson) && lastJson == json) return;

        // 出力先の決定（タスクならサブフォルダ、プロジェクトなら直下）
        var targetDir = isTask ? Path.Combine(changesDir, entityId.ToString()) : changesDir;
        if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

        var fileName = _fileNameGenerator.Generate(timestamp, userId, category);
        var fullPath = Path.Combine(targetDir, fileName);
        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.ReadOnly);
            _lastSavedContent[cacheKey] = json;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving category {Category} for entity {EntityId} to {FileName}.", category, entityId, fileName);
            throw;
        }
    }

    private string GetCacheKey(Guid projectId, string category) => $"{projectId}_{category}";

    public void Dispose()
    {
        _logger.LogInformation("FolderProjectRepository disposing.");
    }
}
