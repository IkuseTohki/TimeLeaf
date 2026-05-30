using System;
using System.Collections.Generic;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// タスクの計画情報を保持するためのDTO。
/// </summary>
internal record TaskPlanningDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    TaskPriority Priority,
    DateTime? ScheduledStartDate,
    DateTime? Deadline,
    double EstimatedCost,
    Guid? Assignee,
    List<TaskConstraintDto> Constraints
);
