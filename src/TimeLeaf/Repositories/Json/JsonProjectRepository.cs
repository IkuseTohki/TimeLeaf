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
        var projectDtos = await JsonSerializer.DeserializeAsync<List<ProjectFullDto>>(stream, _options);
        if (projectDtos == null) return new List<Project>();

        return projectDtos.Select(dto =>
        {
            var project = new Project(
                dto.Id,
                dto.Name,
                dto.Description,
                dto.Status,
                dto.HealthStatus,
                dto.CreatedAt,
                dto.UpdatedAt,
                dto.Tasks?.Select(t => new ProjectTask(
                    t.Id, t.Name, t.Description, t.Status, t.Priority,
                    t.ScheduledStartDate, t.Deadline, t.ActualStartDate, t.ActualEndDate,
                    t.EstimatedCost, t.ActualCost, t.Assignee, t.Dependencies,
                    t.Comments)).ToList(),
                dto.Milestones?.Select(m => new Milestone { Date = m.Date, Label = m.Label }).ToList());
            return project;
        });
    }

    public async System.Threading.Tasks.Task<Project?> LoadAsync(Guid projectId)
    {
        var projects = await LoadAllAsync();
        return projects.FirstOrDefault(p => p.Id == projectId);
    }

    public async System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        var dtos = projects.Select(p => new ProjectFullDto(
            p.Id, p.Name, p.Description, p.Status, p.HealthStatus, p.CreatedAt, p.UpdatedAt,
            p.Tasks.Select(t => new ProjectTaskFullDto(
                t.Id, t.Name, t.Description, t.Status, t.Priority,
                t.ScheduledStartDate, t.Deadline, t.ActualStartDate, t.ActualEndDate,
                t.EstimatedCost, t.ActualCost, t.Assignee, t.Dependencies,
                t.Comments.ToList())).ToList(),
            p.Milestones.Select(m => new MilestoneDto(m.Date, m.Label)).ToList()
        )).ToList();

        using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, dtos, _options);
    }

    public System.Threading.Tasks.Task SaveAsync(Project project)
    {
        return SaveAllAsync(new[] { project });
    }

    private record MilestoneDto(DateTime Date, string Label);
    private record ProjectTaskFullDto(
        Guid Id, string Name, string Description, TimeLeaf.Models.Enums.TaskStatus Status, TimeLeaf.Models.Enums.TaskPriority Priority,
        DateTime? ScheduledStartDate, DateTime? Deadline, DateTime? ActualStartDate, DateTime? ActualEndDate,
        double EstimatedCost, double ActualCost, string Assignee, List<Guid> Dependencies, List<Comment> Comments);
    private record ProjectFullDto(
        Guid Id, string Name, string Description, TimeLeaf.Models.Enums.ProjectStatus Status, TimeLeaf.Models.Enums.ProjectHealth HealthStatus,
        DateTime CreatedAt, DateTime UpdatedAt, List<ProjectTaskFullDto> Tasks, List<MilestoneDto> Milestones);
}
