using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// タスクの進捗情報を保持するためのDTO。
/// </summary>
internal record TaskProgressDto(
    Guid Id,
    TimeLeaf.Models.Enums.TaskStatus Status,
    DateTime? ActualStartDate,
    DateTime? ActualEndDate,
    double ActualCost
);
