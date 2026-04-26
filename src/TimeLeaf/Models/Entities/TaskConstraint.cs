using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスク間の制約（依存関係）を表す値オブジェクト。
/// </summary>
/// <param name="PredecessorId">先行タスクのID。</param>
/// <param name="Type">制約の種類。</param>
/// <param name="LagDays">猶予期間（日）。</param>
/// <param name="Description">制約の理由やメモ。</param>
public record TaskConstraint(
    Guid PredecessorId,
    TaskConstraintType Type = TaskConstraintType.FS,
    int LagDays = 0,
    string Description = ""
);
