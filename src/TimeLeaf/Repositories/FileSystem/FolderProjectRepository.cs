using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly ProjectDirectoryResolver _pathResolver;

    private readonly IProjectStorageCache _cache = new ProjectStorageCache();

    public event Action<Guid>? ProjectChanged;

    public FolderProjectRepository(
        string baseDirectory,
        IProjectStorageMonitor monitor,
        IProjectFileSystemSerializer serializer,
        ICommitFileNameGenerator fileNameGenerator,
        ILogger<FolderProjectRepository> logger
    )
    {
        _baseDirectory = baseDirectory;
        _monitor = monitor;
        _serializer = serializer;
        _fileNameGenerator = fileNameGenerator;
        _logger = logger;
        _pathResolver = new ProjectDirectoryResolver(baseDirectory);

        _logger.LogInformation(
            "FolderProjectRepository initializing with base directory: {BaseDirectory}",
            _baseDirectory
        );

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

                Guid createdBy = Guid.Empty;
                if (Guid.TryParse(meta.CreatedBy, out var cbGuid))
                {
                    createdBy = cbGuid;
                }

                // .project から判明している ID を渡して Replay を開始
                var project = await ReplayProjectAsync(dir, meta.ProjectId, meta.CreatedAt, createdBy);

                // ライフサイクル状態を適用
                if (project != null)
                {
                    project.SetLifecycleStatus(meta.IsArchived, meta.LockedUntil);
                }

                return project != null ? new { Project = project, CreatedAt = meta.CreatedAt } : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading project metadata from {MetaPath}. Skipping.", metaPath);
                return null;
            }
        });

        var results = await System.Threading.Tasks.Task.WhenAll(projectTasks);
        _logger.LogInformation(
            "Finished loading all projects. Found {LoadedProjectCount} valid projects.",
            results.Count(r => r != null)
        );

        return results.Where(r => r != null).OrderBy(r => r!.CreatedAt).Select(r => r!.Project).ToList();
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
        DateTime createdAt = DateTime.Now;
        bool isArchived = false;
        DateTime? lockedUntil = null;

        Guid createdBy = Guid.Empty;

        if (File.Exists(metaPath))
        {
            var metaJson = await File.ReadAllTextAsync(metaPath);
            var meta = _serializer.Deserialize<ProjectMetadataDto>(metaJson);
            if (meta != null)
            {
                createdAt = meta.CreatedAt;
                isArchived = meta.IsArchived;
                lockedUntil = meta.LockedUntil;
                if (Guid.TryParse(meta.CreatedBy, out var cbGuid))
                {
                    createdBy = cbGuid;
                }
            }
        }

        var project = await ReplayProjectAsync(targetDir, projectId, createdAt, createdBy);
        if (project != null)
        {
            project.SetLifecycleStatus(isArchived, lockedUntil);
        }
        return project;
    }

    private async Task<Project?> ReplayProjectAsync(
        string projectDirPath,
        Guid projectId,
        DateTime createdAt,
        Guid createdBy
    )
    {
        var changesDir = Path.Combine(projectDirPath, "changes");
        if (!Directory.Exists(changesDir))
        {
            _logger.LogWarning(
                "Changes directory {ChangesDir} not found for {ProjectId}. Returning new Project.",
                changesDir,
                projectId
            );
            return new Project(createdBy) { Id = projectId, CreatedAt = createdAt };
        }

        var replayer = new ProjectHistoryReplayer(_serializer, _fileNameGenerator, _logger, _cache);
        return await replayer.ReplayAsync(changesDir, projectId, createdAt, createdBy);
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects, string userId)
    {
        foreach (var project in projects)
            await SaveAsync(project, userId);
    }

    /// <summary>
    /// 指定されたプロジェクトを物理的に削除します。
    /// </summary>
    public async System.Threading.Tasks.Task DeleteAsync(Guid projectId)
    {
        _logger.LogWarning("Deleting project: {ProjectId}", projectId);
        var projectDirs = Directory.GetDirectories(_baseDirectory);
        var targetDir = projectDirs.FirstOrDefault(d => Path.GetFileName(d).StartsWith(projectId.ToString()));

        if (targetDir != null && Directory.Exists(targetDir))
        {
            try
            {
                // ディレクトリごと削除 (再帰的)
                await System.Threading.Tasks.Task.Run(() => Directory.Delete(targetDir, true));
                _logger.LogInformation("Deleted project directory: {TargetDir}", targetDir);

                // 変更通知を発火（削除されたことを通知）
                OnMonitorProjectChanged(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete project directory: {TargetDir}", targetDir);
                throw;
            }
        }
        else
        {
            _logger.LogWarning("Project directory not found for deletion: {ProjectId}", projectId);
        }
    }

    /// <summary>
    /// 指定されたエンティティの変更履歴をディレクトリからスキャンして取得します。
    /// </summary>
    public async System.Threading.Tasks.Task<IEnumerable<ChangeRecord>> GetHistoryAsync(Guid projectId, Guid entityId)
    {
        _logger.LogDebug("Fetching history for entity {EntityId} in project {ProjectId}.", entityId, projectId);

        var projectDirs = Directory.GetDirectories(_baseDirectory);
        var targetDir = projectDirs.FirstOrDefault(d => Path.GetFileName(d).StartsWith(projectId.ToString()));

        if (targetDir == null)
        {
            _logger.LogWarning("Project directory for {ProjectId} not found during history fetch.", projectId);
            return Enumerable.Empty<ChangeRecord>();
        }

        var changesDir = Path.Combine(targetDir, "changes");
        // プロジェクト自身なら changes 直下、タスク等ならサブフォルダ
        var entityDir = entityId == projectId ? changesDir : Path.Combine(changesDir, entityId.ToString());

        if (!Directory.Exists(entityDir))
        {
            _logger.LogDebug("Entity directory {EntityDir} not found. No history available.", entityDir);
            return Enumerable.Empty<ChangeRecord>();
        }

        // 非同期でファイル一覧を取得（I/O負荷軽減のためスレッドプールで実行）
        var files = await System.Threading.Tasks.Task.Run(() =>
            Directory.GetFiles(entityDir, "*.json", SearchOption.TopDirectoryOnly)
        );

        var records = new List<ChangeRecord>();
        foreach (var f in files)
        {
            var fileName = Path.GetFileName(f);
            var meta = _fileNameGenerator.Parse(fileName);
            Guid userId = Guid.TryParse(meta.UserId, out var id) ? id : Guid.Empty;

            // 詳細表示のためにJSONコンテンツを読み込む
            string? content = null;
            try
            {
                content = await File.ReadAllTextAsync(f);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read history file content: {FilePath}", f);
            }

            records.Add(new ChangeRecord(meta.Timestamp, userId, meta.Category, content));
        }

        records = records.OrderBy(r => r.Timestamp).ToList();

        _logger.LogDebug("Found {HistoryCount} history records for entity {EntityId}.", records.Count, entityId);
        return records;
    }

    public async System.Threading.Tasks.Task SaveAsync(Project project, string userId)
    {
        _logger.LogInformation("Saving project: {ProjectName} ({ProjectId})", project.Name, project.Id);
        try
        {
            var projectDir = _pathResolver.GetProjectDirectory(project.Id, project.Name);
            if (!Directory.Exists(projectDir))
            {
                Directory.CreateDirectory(projectDir);
            }

            var changesDir = _pathResolver.GetChangesDirectory(project.Id, project.Name);
            if (!Directory.Exists(changesDir))
            {
                Directory.CreateDirectory(changesDir);
            }

            // .project (Metadata) の保存・更新
            var metaFilePath = _pathResolver.GetMetadataFilePath(project.Id, project.Name);
            var newMetadata = new ProjectMetadataDto(
                project.Id,
                project.CreatedAt,
                userId,
                1,
                project.IsArchived,
                project.LockedUntil
            );

            bool shouldWriteMetadata = true;
            if (File.Exists(metaFilePath))
            {
                try
                {
                    var existingJson = await File.ReadAllTextAsync(metaFilePath);
                    var existingMeta = _serializer.Deserialize<ProjectMetadataDto>(existingJson);
                    if (existingMeta != null && existingMeta == newMetadata)
                    {
                        shouldWriteMetadata = false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read existing .project file. Overwriting.");
                }
            }

            if (shouldWriteMetadata)
            {
                await File.WriteAllTextAsync(metaFilePath, _serializer.Serialize(newMetadata));
            }

            var commitTime = DateTime.Now;

            // 1. 削除マーカー（Tombstone）の生成
            var deletedIds = project.DeletedTaskIds.Concat(project.DeletedContainerIds);
            foreach (var entityId in deletedIds)
            {
                var entityDir = _pathResolver.GetEntityDirectory(changesDir, entityId);
                if (!Directory.Exists(entityDir))
                    Directory.CreateDirectory(entityDir);

                var tombstoneFileName = _fileNameGenerator.Generate(
                    commitTime,
                    userId,
                    StorageCategories.Deleted,
                    entityId
                );
                var tombstonePath = Path.Combine(entityDir, tombstoneFileName);

                if (!File.Exists(tombstonePath))
                {
                    await File.WriteAllTextAsync(tombstonePath, "{}");
                    _monitor.MarkFileAsJustWritten(tombstoneFileName);
                    _logger.LogInformation("Created tombstone for entity {EntityId}.", entityId);
                }
            }

            // 2. プロジェクト情報の保存 (4カテゴリ)
            await TrySaveCategoryAsync(
                project.Id,
                changesDir,
                StorageCategories.ProjectBasic,
                new ProjectBasicDto(project.Name, project.Status),
                commitTime,
                userId,
                isEntity: false
            );

            await TrySaveCategoryAsync(
                project.Id,
                changesDir,
                StorageCategories.ProjectDescription,
                new ProjectDescriptionDto(project.Description),
                commitTime,
                userId,
                isEntity: false
            );

            await TrySaveCategoryAsync(
                project.Id,
                changesDir,
                StorageCategories.ProjectMilestones,
                new ProjectMilestonesDto(project.Milestones.Select(m => new MilestoneDto(m.Date, m.Label)).ToList()),
                commitTime,
                userId,
                isEntity: false
            );

            await TrySaveCategoryAsync(
                project.Id,
                changesDir,
                StorageCategories.ProjectMembers,
                new ProjectMembersDto { AssignedUserIds = project.AssignedUserIds.ToList() },
                commitTime,
                userId,
                isEntity: false
            );

            // 2.1 表示順序の保存 (コンテナ -> タスクの順で現在の物理的な並びを保存)
            var orderedIds = project.Containers.Select(c => c.Id).Concat(project.Tasks.Select(t => t.Id)).ToList();
            if (orderedIds.Any())
            {
                await TrySaveCategoryAsync(
                    project.Id,
                    changesDir,
                    StorageCategories.ProjectSortOrder,
                    new ProjectSortOrderDto(orderedIds),
                    commitTime,
                    userId,
                    isEntity: false
                );
            }

            // 3. コンテナ情報の保存
            foreach (var container in project.Containers)
            {
                var planning = new ContainerPlanningDto(
                    container.Id,
                    container.ParentId,
                    container.Name,
                    container.PlannedStartDate,
                    container.PlannedEndDate,
                    container.Deadline,
                    container.RequiredDays,
                    container
                        .Constraints.Select(c => new TaskConstraintDto(
                            c.PredecessorId,
                            c.Type,
                            c.LagDays,
                            c.Description
                        ))
                        .ToList(),
                    container.Children.Select(c => c.Id).ToList()
                );
                await TrySaveCategoryAsync(
                    container.Id,
                    changesDir,
                    StorageCategories.ContainerPlanning,
                    planning,
                    commitTime,
                    userId,
                    isEntity: true
                );

                var containerDesc = new ContainerDescriptionDto(container.Id, container.Description);
                await TrySaveCategoryAsync(
                    container.Id,
                    changesDir,
                    StorageCategories.ContainerDescription,
                    containerDesc,
                    commitTime,
                    userId,
                    isEntity: true
                );

                var relations = new ContainerRelationsDto(
                    container.Id,
                    container.WatcherIds.ToList(),
                    container.RelatedTaskIds.ToList()
                );
                await TrySaveCategoryAsync(
                    container.Id,
                    changesDir,
                    StorageCategories.ContainerRelations,
                    relations,
                    commitTime,
                    userId,
                    isEntity: true
                );
            }

            // 4. タスク情報の保存 (各タスク 3カテゴリ)
            foreach (var task in project.Tasks)
            {
                var planning = new TaskPlanningDto(
                    task.Id,
                    task.ParentId,
                    task.Name,
                    task.Priority,
                    task.PlannedStartDate,
                    task.Deadline,
                    task.EstimatedCost,
                    task.Assignee,
                    task.Constraints.Select(c => new TaskConstraintDto(
                            c.PredecessorId,
                            c.Type,
                            c.LagDays,
                            c.Description
                        ))
                        .ToList()
                );
                await TrySaveCategoryAsync(
                    task.Id,
                    changesDir,
                    StorageCategories.TaskPlanning,
                    planning,
                    commitTime,
                    userId,
                    isEntity: true
                );

                var progress = new TaskProgressDto(
                    task.Id,
                    task.Status,
                    task.ActualStartDate,
                    task.ActualEndDate,
                    task.ActualCost
                );
                await TrySaveCategoryAsync(
                    task.Id,
                    changesDir,
                    StorageCategories.TaskProgress,
                    progress,
                    commitTime,
                    userId,
                    isEntity: true
                );

                var taskDesc = new TaskDescriptionDto(task.Id, task.Description);
                await TrySaveCategoryAsync(
                    task.Id,
                    changesDir,
                    StorageCategories.TaskDescription,
                    taskDesc,
                    commitTime,
                    userId,
                    isEntity: true
                );

                // 5. コメントの保存 (各コメント 1ファイル)
                foreach (var comment in task.Comments)
                {
                    await TrySaveCommentAsync(project.Id, task.Id, changesDir, comment, userId);
                }
            }

            // 保存完了後、ドメインの状態をリセット
            project.ClearDeletedIds();
            project.SetUpdatedAt(commitTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project {ProjectName} ({ProjectId})", project.Name, project.Id);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TrySaveCommentAsync(
        Guid projectId,
        Guid taskId,
        string changesDir,
        Comment comment,
        string userId
    )
    {
        // 診断用ログ
        _logger.LogInformation(
            "Saving comment. Cache size: {Count}, TaskId: {TaskId}, CommentId: {CommentId}",
            _cache.Count,
            taskId,
            comment.Id
        );

        // メモリキャッシュによる重複チェック
        if (_cache.TryGetComment(taskId, comment.Id, out _))
        {
            _logger.LogInformation("Cache hit for comment {CommentId}", comment.Id);
            return;
        }

        // タスクIDごとのサブフォルダを作成
        var taskDir = Path.Combine(changesDir, taskId.ToString());
        var fileName = _fileNameGenerator.Generate(comment.CreatedAt, userId, StorageCategories.Comment);
        var fullPath = Path.Combine(taskDir, fileName);

        // 物理ファイルによる重複チェック
        if (File.Exists(fullPath))
        {
            _logger.LogInformation("Physical file exists for comment {CommentId}, skipping.", comment.Id);
            return;
        }

        if (!Directory.Exists(taskDir))
            Directory.CreateDirectory(taskDir);

        var commentDto = new CommentDto(
            comment.Id,
            comment.TaskId,
            comment.AuthorId,
            comment.Content,
            comment.AttachmentLinks
        );
        var json = _serializer.Serialize(commentDto);

        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            try
            {
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not set ReadOnly attribute for {FileName}, but file was saved.",
                    fileName
                );
            }
            _cache.UpdateComment(taskId, comment.Id, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving incremental comment {CommentId} to {FileName}.", comment.Id, fileName);
            throw;
        }
    }

    private async System.Threading.Tasks.Task TrySaveCategoryAsync(
        Guid entityId,
        string changesDir,
        string category,
        object data,
        DateTime timestamp,
        string userId,
        bool isEntity = false
    )
    {
        var json = _serializer.Serialize(data);

        if (_cache.TryGetCategory(entityId, category, out var lastJson) && lastJson == json)
            return;

        // 出力先の決定（タスク/コンテナならサブフォルダ、プロジェクトなら直下）
        var targetDir = isEntity ? Path.Combine(changesDir, entityId.ToString()) : changesDir;
        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        var fileName = _fileNameGenerator.Generate(timestamp, userId, category);
        var fullPath = Path.Combine(targetDir, fileName);
        _monitor.MarkFileAsJustWritten(fileName);

        try
        {
            await File.WriteAllTextAsync(fullPath, json);
            try
            {
                File.SetAttributes(fullPath, File.GetAttributes(fullPath) | FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Could not set ReadOnly attribute for {FileName}, but file was saved.",
                    fileName
                );
            }
            _cache.UpdateCategory(entityId, category, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving category {Category} for entity {EntityId} to {FileName}.",
                category,
                entityId,
                fileName
            );
            throw;
        }
    }

    public void Dispose() { }
}
