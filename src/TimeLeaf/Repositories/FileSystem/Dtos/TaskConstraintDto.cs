using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// タスク間の制約情報を保持するためのDTO。
/// </summary>
internal record TaskConstraintDto(Guid PredecessorId, TaskConstraintType Type, int LagDays, string Description);
