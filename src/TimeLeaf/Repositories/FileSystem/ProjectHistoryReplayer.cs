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
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _lastSavedContent;

    public ProjectHistoryReplayer(
        IProjectFileSystemSerializer serializer,
        ICommitFileNameGenerator fileNameGenerator,
        ILogger logger,
        System.Collections.Concurrent.ConcurrentDictionary<string, string> lastSavedContent)
    {
        _serializer = serializer;
        _fileNameGenerator = fileNameGenerator;
        _logger = logger;
        _lastSavedContent = lastSavedContent;
    }

    /// <summary>
    /// 指定されたディレクトリ内の履歴ファイルをスキャンし、プロジェクトの状態を復元します。
    /// </summary>
    public async Task<Project> ReplayAsync(string changesDir, Guid projectId, DateTime createdAt)
    {
        var files = ScanChangeFiles(changesDir, projectId);
        _logger.LogDebug("Found {ChangeFileCount} change files for project {ProjectId}.", files.Count, projectId);

        var project = new Project { Id = projectId, CreatedAt = createdAt };
        var allCommentData = new List<(CommentDto Dto, DateTime Timestamp)>();
        var taskMap = new Dictionary<Guid, ProjectTask>();

        foreach (var file in files)
        {
            await ApplyChangeAsync(project, taskMap, allCommentData, file.Path, file.EntityId, file.Meta);
        }

        AttachComments(taskMap, allCommentData);

        if (files.Any())
        {
            project.SetUpdatedAt(files.Last().Meta.Timestamp);
        }

        return project;
    }

    private List<(string Path, CommitFileName Meta, Guid EntityId)> ScanChangeFiles(string changesDir, Guid projectId)
    {
        return Directory.GetFiles(changesDir, "*.json", SearchOption.AllDirectories)
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

    private async Task ApplyChangeAsync(Project project, Dictionary<Guid, ProjectTask> taskMap, List<(CommentDto Dto, DateTime Timestamp)> allCommentData, string filePath, Guid entityId, CommitFileName meta)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var cacheKey = $"{entityId}_{meta.Category}";

        if (meta.Category != "Comment")
        {
            _lastSavedContent[cacheKey] = json;
        }

        switch (meta.Category)
        {
            case "Project_Basic": ApplyProjectBasic(project, json); break;
            case "Project_Description": ApplyProjectDescription(project, json); break;
            case "Project_Milestones": ApplyProjectMilestones(project, json); break;
            case "Project_Members": ApplyProjectMembers(project, json); break;
            case "Task_Planning": ApplyTaskPlanning(project, taskMap, json); break;
            case "Task_Progress": ApplyTaskProgress(project, taskMap, json); break;
            case "Task_Description": ApplyTaskDescription(project, taskMap, json); break;
            case "Comment": AccumulateComment(allCommentData, json, meta.Timestamp); break;
        }
    }

    private void ApplyProjectBasic(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectBasicDto>(json);
        if (dto == null) return;
        project.UpdateBasicInfo(dto.Name, dto.Status, dto.HealthStatus);
    }

    private void ApplyProjectDescription(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectDescriptionDto>(json);
        if (dto != null) project.UpdateDescription(dto.Description);
    }

    private void ApplyProjectMilestones(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectMilestonesDto>(json);
        if (dto == null) return;
        project.ReplayMilestones(dto.Milestones.Select(m => new Milestone { Date = m.Date, Label = m.Label }));
    }

    private void ApplyProjectMembers(Project project, string json)
    {
        var dto = _serializer.Deserialize<ProjectMembersDto>(json);
        if (dto == null) return;
        project.ReplayAssignments(dto.AssignedUserIds);
    }

    private void ApplyTaskPlanning(Project project, Dictionary<Guid, ProjectTask> taskMap, string json)
    {
        var dto = _serializer.Deserialize<TaskPlanningDto>(json);
        if (dto == null) return;
        var task = GetOrCreateTask(project, taskMap, dto.Id);
        task.UpdateName(dto.Name);
        task.UpdatePriority(dto.Priority);
        task.UpdateSchedule(dto.ScheduledStartDate, dto.Deadline);
        task.UpdateEstimatedCost(dto.EstimatedCost);
        task.AssignTo(dto.Assignee);
        task.Dependencies.Clear();
        if (dto.Dependencies != null) task.Dependencies.AddRange(dto.Dependencies);
    }

    private void ApplyTaskProgress(Project project, Dictionary<Guid, ProjectTask> taskMap, string json)
    {
        var dto = _serializer.Deserialize<TaskProgressDto>(json);
        if (dto == null) return;
        var task = GetOrCreateTask(project, taskMap, dto.Id);
        task.UpdateStatus(dto.Status);
        task.UpdateActualDates(dto.ActualStartDate, dto.ActualEndDate);
        task.UpdateActualCost(dto.ActualCost);
    }

    private void ApplyTaskDescription(Project project, Dictionary<Guid, ProjectTask> taskMap, string json)
    {
        var dto = _serializer.Deserialize<TaskDescriptionDto>(json);
        if (dto == null) return;
        var task = GetOrCreateTask(project, taskMap, dto.Id);
        task.UpdateDescription(dto.Description);
    }

    private void AccumulateComment(List<(CommentDto Dto, DateTime Timestamp)> allCommentData, string json, DateTime timestamp)
    {
        var dto = _serializer.Deserialize<CommentDto>(json);
        if (dto != null)
        {
            allCommentData.Add((dto, timestamp));
            _lastSavedContent[$"{dto.Id}_Comment"] = json;
        }
    }

    private void AttachComments(Dictionary<Guid, ProjectTask> taskMap, List<(CommentDto Dto, DateTime Timestamp)> allCommentData)
    {
        foreach (var entry in allCommentData)
        {
            var dto = entry.Dto;
            if (taskMap.TryGetValue(dto.TaskId, out var targetTask))
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
    }

    private ProjectTask GetOrCreateTask(Project project, Dictionary<Guid, ProjectTask> map, Guid taskId)
    {
        if (map.TryGetValue(taskId, out var task)) return task;
        var newTask = new ProjectTask { Id = taskId };
        project.AddTask(newTask);
        map[taskId] = newTask;
        return newTask;
    }
}
