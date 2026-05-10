using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// プロジェクトの変更履歴を読み込み、エンティティの状態を復元（Replay）する責務を持つクラス。
/// </summary>
internal class ProjectHistoryReplayer
{
    private readonly IProjectFileSystemSerializer _serializer;
    private readonly ICommitFileNameGenerator _fileNameGenerator;
    private readonly ILogger _logger;
    private readonly IProjectStorageCache _cache;

    public ProjectHistoryReplayer(
        IProjectFileSystemSerializer serializer,
        ICommitFileNameGenerator fileNameGenerator,
        ILogger logger,
        IProjectStorageCache cache
    )
    {
        _serializer = serializer;
        _fileNameGenerator = fileNameGenerator;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// 指定されたディレクトリ内の履歴ファイルをスキャンし、プロジェクトの状態を復元します。
    /// </summary>
    public async Task<Project> ReplayAsync(string changesDir, Guid projectId, DateTime createdAt, Guid createdBy)
    {
        var files = ScanChangeFiles(changesDir, projectId);

        // 削除マーカー(Tombstone)の検出
        var deletedEntities = new HashSet<Guid>();
        var tombstoneFiles = files.Where(f => f.Meta.Category == StorageCategories.Deleted).ToList();
        foreach (var tombstone in tombstoneFiles)
        {
            deletedEntities.Add(tombstone.EntityId);
        }

        _logger.LogDebug("Found {ChangeFileCount} change files for project {ProjectId}.", files.Count, projectId);

        var project = new Project(createdBy) { Id = projectId, CreatedAt = createdAt };
        var allCommentData = new List<(CommentDto Dto, DateTime Timestamp)>();
        var taskMap = new Dictionary<Guid, ProjectTask>();
        var containerMap = new Dictionary<Guid, ProjectContainer>();
        var workItemMap = new Dictionary<Guid, ProjectWorkItem>();
        string? latestSortOrderJson = null;

        foreach (var file in files)
        {
            // 削除済みエンティティの変更は無視
            if (deletedEntities.Contains(file.EntityId))
                continue;

            if (file.Meta.Category == StorageCategories.ProjectSortOrder)
            {
                // 最新の順序ファイルのみを記憶しておく
                latestSortOrderJson = await File.ReadAllTextAsync(file.Path);
                _cache.UpdateCategory(file.EntityId, file.Meta.Category, latestSortOrderJson);
                continue;
            }

            await ApplyChangeAsync(
                project,
                taskMap,
                containerMap,
                workItemMap,
                allCommentData,
                file.Path,
                file.EntityId,
                file.Meta
            );
        }

        // 表示順序の適用（全アイテムの復元が完了した後に行う）
        if (latestSortOrderJson != null)
        {
            var dto = _serializer.Deserialize<ProjectSortOrderDto>(latestSortOrderJson);
            if (dto != null && dto.OrderedIds != null)
            {
                project.ReorderWorkItems(dto.OrderedIds);
            }
        }

        AttachComments(taskMap, allCommentData);
        ResolveHierarchy(workItemMap);

        if (files.Any())
        {
            project.SetUpdatedAt(files.Last().Meta.Timestamp);
        }

        return project;
    }

    private void ResolveHierarchy(Dictionary<Guid, ProjectWorkItem> workItemMap)
    {
        foreach (var item in workItemMap.Values.ToList())
        {
            if (item.ParentId.HasValue && workItemMap.TryGetValue(item.ParentId.Value, out var parent))
            {
                if (parent is ProjectContainer pc)
                {
                    if (!pc.Children.Any(c => c.Id == item.Id))
                    {
                        pc.AddChild(item);
                    }
                }
                else if (parent is ProjectTask pt && item is ProjectTask ct)
                {
                    // 互換性のため、タスク間の親子関係も維持
                    if (!pt.Children.Any(c => c.Id == ct.Id))
                    {
                        pt.AddChild(ct);
                    }
                }
            }
        }
    }

    private List<(string Path, CommitFileName Meta, Guid EntityId)> ScanChangeFiles(string changesDir, Guid projectId)
    {
        return Directory
            .GetFiles(changesDir, "*.json", SearchOption.AllDirectories)
            .Select(f =>
            {
                var fileName = Path.GetFileName(f);
                var meta = _fileNameGenerator.Parse(fileName);
                var parentDirName = Path.GetFileName(Path.GetDirectoryName(f)!);
                var entityId = Guid.TryParse(parentDirName, out var id) ? id : projectId;

                return (Path: f, Meta: meta, EntityId: entityId);
            })
            .OrderBy(x => x.Meta.Timestamp)
            .ToList();
    }

    private async Task ApplyChangeAsync(
        Project project,
        Dictionary<Guid, ProjectTask> taskMap,
        Dictionary<Guid, ProjectContainer> containerMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        List<(CommentDto Dto, DateTime Timestamp)> allCommentData,
        string filePath,
        Guid entityId,
        CommitFileName meta
    )
    {
        var json = await File.ReadAllTextAsync(filePath);

        // すべてのカテゴリをキャッシュする
        if (meta.Category != StorageCategories.Comment)
        {
            _cache.UpdateCategory(entityId, meta.Category, json);
        }

        switch (meta.Category)
        {
            case StorageCategories.ProjectBasic:
                ApplyProjectBasic(project, json);
                break;
            case StorageCategories.ProjectDescription:
                ApplyProjectDescription(project, json);
                break;
            case StorageCategories.ProjectMilestones:
                ApplyProjectMilestones(project, json);
                break;
            case StorageCategories.ProjectMembers:
                ApplyProjectMembers(project, json);
                break;
            case StorageCategories.ContainerPlanning:
                ApplyContainerPlanning(project, containerMap, workItemMap, json);
                break;
            case StorageCategories.ContainerDescription:
                ApplyContainerDescription(project, containerMap, workItemMap, json);
                break;
            case StorageCategories.ContainerRelations:
                ApplyContainerRelations(project, containerMap, workItemMap, json);
                break;
            case StorageCategories.TaskPlanning:
                ApplyTaskPlanning(project, taskMap, workItemMap, json);
                break;
            case StorageCategories.TaskProgress:
                ApplyTaskProgress(project, taskMap, workItemMap, json);
                break;
            case StorageCategories.TaskDescription:
                ApplyTaskDescription(project, taskMap, workItemMap, json);
                break;
            case StorageCategories.Comment:
                AccumulateComment(allCommentData, json, meta.Timestamp);
                break;
        }
    }

    private void ApplyProjectBasic(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectBasicDto>(json);
        if (dto == null)
            return;
        project.UpdateBasicInfo(dto.Name, dto.Status);
    }

    private void ApplyProjectDescription(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectDescriptionDto>(json);
        if (dto != null)
            project.UpdateDescription(dto.Description);
    }

    private void ApplyProjectMilestones(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectMilestonesDto>(json);
        if (dto == null)
            return;
        project.ReplayMilestones(dto.Milestones.Select(m => new Milestone { Date = m.Date, Label = m.Label }));
    }

    private void ApplyProjectMembers(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectMembersDto>(json);
        if (dto == null)
            return;
        project.ReplayAssignments(dto.AssignedUserIds);
    }

    private void ApplyContainerPlanning(
        Project project,
        Dictionary<Guid, ProjectContainer> containerMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<ContainerPlanningDto>(json);
        if (dto == null)
            return;
        var container = GetOrCreateContainer(project, containerMap, workItemMap, dto.Id);
        container.SetParentId(dto.ParentId);
        container.UpdateName(dto.Name);
        container.UpdateSchedule(dto.PlannedStartDate, dto.PlannedEndDate, dto.Deadline);
        container.UpdateRequiredDays(dto.RequiredDays);

        if (dto.Constraints != null)
        {
            var constraints = dto.Constraints.Select(c => new TaskConstraint(
                c.PredecessorId,
                c.Type,
                c.LagDays,
                c.Description
            ));
            container.LoadConstraints(constraints);
        }
    }

    private void ApplyContainerDescription(
        Project project,
        Dictionary<Guid, ProjectContainer> containerMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<ContainerDescriptionDto>(json);
        if (dto == null)
            return;
        var container = GetOrCreateContainer(project, containerMap, workItemMap, dto.Id);
        container.UpdateDescription(dto.Description);
    }

    private void ApplyContainerRelations(
        Project project,
        Dictionary<Guid, ProjectContainer> containerMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<ContainerRelationsDto>(json);
        if (dto == null)
            return;
        var container = GetOrCreateContainer(project, containerMap, workItemMap, dto.Id);
        // 現時点では WatcherIds 等の復元のみ
    }

    private void ApplyTaskPlanning(
        Project project,
        Dictionary<Guid, ProjectTask> taskMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<TaskPlanningDto>(json);
        if (dto == null)
            return;
        var task = GetOrCreateTask(project, taskMap, workItemMap, dto.Id);
        task.SetParentId(dto.ParentId);
        task.UpdateName(dto.Name);
        task.UpdatePriority(dto.Priority);
        task.UpdateSchedule(dto.ScheduledStartDate, dto.Deadline);
        task.UpdateEstimatedCost(dto.EstimatedCost);
        task.AssignTo(dto.Assignee);

        if (dto.Constraints != null)
        {
            var constraints = dto.Constraints.Select(c => new TaskConstraint(
                c.PredecessorId,
                c.Type,
                c.LagDays,
                c.Description
            ));
            task.LoadConstraints(constraints);
        }
    }

    private void ApplyTaskProgress(
        Project project,
        Dictionary<Guid, ProjectTask> taskMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<TaskProgressDto>(json);
        if (dto == null)
            return;
        var task = GetOrCreateTask(project, taskMap, workItemMap, dto.Id);
        task.UpdateStatus(dto.Status);
        task.UpdateActualDates(dto.ActualStartDate, dto.ActualEndDate);
        task.UpdateActualCost(dto.ActualCost);
    }

    private void ApplyTaskDescription(
        Project project,
        Dictionary<Guid, ProjectTask> taskMap,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        string json
    )
    {
        var dto = _serializer.Deserialize<TaskDescriptionDto>(json);
        if (dto == null)
            return;
        var task = GetOrCreateTask(project, taskMap, workItemMap, dto.Id);
        task.UpdateDescription(dto.Description);
    }

    private void AccumulateComment(
        List<(CommentDto Dto, DateTime Timestamp)> allCommentData,
        string json,
        DateTime timestamp
    )
    {
        var dto = _serializer.Deserialize<CommentDto>(json);
        if (dto != null)
        {
            allCommentData.Add((dto, timestamp));
            _cache.UpdateComment(dto.TaskId, dto.Id, json);
        }
    }

    private void AttachComments(
        Dictionary<Guid, ProjectTask> taskMap,
        List<(CommentDto Dto, DateTime Timestamp)> allCommentData
    )
    {
        foreach (var entry in allCommentData)
        {
            var dto = entry.Dto;
            if (taskMap.TryGetValue(dto.TaskId, out var targetTask))
            {
                if (!targetTask.Comments.Any(c => c.Id == dto.Id))
                {
                    targetTask.AddComment(
                        new Comment
                        {
                            Id = dto.Id,
                            TaskId = dto.TaskId,
                            AuthorId = dto.AuthorId,
                            CreatedAt = entry.Timestamp,
                            Content = dto.Content,
                            AttachmentLinks = dto.AttachmentLinks ?? new(),
                        }
                    );
                }
            }
        }
    }

    private ProjectContainer GetOrCreateContainer(
        Project project,
        Dictionary<Guid, ProjectContainer> map,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        Guid containerId
    )
    {
        if (map.TryGetValue(containerId, out var container))
            return container;
        var newContainer = new ProjectContainer { Id = containerId };
        project.AddContainer(newContainer);
        map[containerId] = newContainer;
        workItemMap[containerId] = newContainer;
        return newContainer;
    }

    private ProjectTask GetOrCreateTask(
        Project project,
        Dictionary<Guid, ProjectTask> map,
        Dictionary<Guid, ProjectWorkItem> workItemMap,
        Guid taskId
    )
    {
        if (map.TryGetValue(taskId, out var task))
            return task;
        var newTask = new ProjectTask { Id = taskId };
        project.AddTask(newTask);
        map[taskId] = newTask;
        workItemMap[taskId] = newTask;
        return newTask;
    }
}
