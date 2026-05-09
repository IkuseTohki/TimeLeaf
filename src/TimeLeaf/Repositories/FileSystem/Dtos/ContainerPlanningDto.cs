using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// コンテナの計画情報を保持するためのDTO。
/// </summary>
internal record ContainerPlanningDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    DateTime? PlannedStartDate,
    DateTime? PlannedEndDate,
    DateTime? Deadline,
    double RequiredDays,
    List<TaskConstraintDto> Constraints
);
