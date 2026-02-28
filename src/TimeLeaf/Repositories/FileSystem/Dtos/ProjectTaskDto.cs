using System;
using System.Collections.Generic;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// タスク情報を保持するためのDTO（スナップショット用）。
/// </summary>
internal record ProjectTaskDto(
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
