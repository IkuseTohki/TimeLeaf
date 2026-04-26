using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクト全体のリスクをスキャンして検出するユースケース。
/// </summary>
public class DetectProjectRisksUseCase
{
    private readonly CalculateCriticalPathUseCase _calculateCriticalPath;

    public DetectProjectRisksUseCase(CalculateCriticalPathUseCase calculateCriticalPath)
    {
        _calculateCriticalPath = calculateCriticalPath;
    }

    /// <summary>
    /// プロジェクトのリスクを検出し、リストとして返します。
    /// </summary>
    public virtual async Task<IEnumerable<ProjectRisk>> ExecuteAsync(Project project)
    {
        var risks = new List<ProjectRisk>();

        // 1. 循環参照
        var validation = project.ValidateConstraints();
        if (!validation.IsValid)
        {
            foreach (var err in validation.Errors)
            {
                risks.Add(new ProjectRisk(RiskType.CycleDetected, err, IsError: true));
            }
            return risks;
        }

        // 2. タスク単位の検証（制約違反、期限切れ）
        foreach (var task in project.Tasks)
        {
            // 期限切れ
            if (
                task.Status != TimeLeaf.Models.Enums.TaskStatus.Completed
                && task.Deadline.HasValue
                && task.Deadline.Value.Date < DateTime.Today
            )
            {
                risks.Add(new ProjectRisk(RiskType.Overdue, $"タスク「{task.Name}」の期限が過ぎています。", task.Id));
            }

            // 制約違反
            foreach (var constraint in task.Constraints)
            {
                var pred = project.Tasks.FirstOrDefault(t => t.Id == constraint.PredecessorId);
                if (pred == null)
                    continue;

                if (constraint.Type == TaskConstraintType.FS)
                {
                    if (
                        pred.Status != TimeLeaf.Models.Enums.TaskStatus.Completed
                        && task.Status != TimeLeaf.Models.Enums.TaskStatus.NotStarted
                    )
                    {
                        risks.Add(
                            new ProjectRisk(
                                RiskType.ConstraintViolation,
                                $"先行タスク「{pred.Name}」が未完了ですが、後続の「{task.Name}」が開始されています。",
                                task.Id
                            )
                        );
                    }
                }
                else if (constraint.Type == TaskConstraintType.SS)
                {
                    if (
                        pred.Status == TimeLeaf.Models.Enums.TaskStatus.NotStarted
                        && task.Status != TimeLeaf.Models.Enums.TaskStatus.NotStarted
                    )
                    {
                        risks.Add(
                            new ProjectRisk(
                                RiskType.ConstraintViolation,
                                $"先行タスク「{pred.Name}」が未着手ですが、後続の「{task.Name}」が開始されています。",
                                task.Id
                            )
                        );
                    }
                }
                else if (constraint.Type == TaskConstraintType.FF)
                {
                    if (
                        pred.Status != TimeLeaf.Models.Enums.TaskStatus.Completed
                        && task.Status == TimeLeaf.Models.Enums.TaskStatus.Completed
                    )
                    {
                        risks.Add(
                            new ProjectRisk(
                                RiskType.ConstraintViolation,
                                $"先行タスク「{pred.Name}」が未完了ですが、後続の「{task.Name}」が完了しています。",
                                task.Id
                            )
                        );
                    }
                }
            }
        }

        // 3. 遅延リスク（クリティカルパス）
        var criticalIds = await _calculateCriticalPath.ExecuteAsync(project);
        var criticalIdSet = new HashSet<Guid>(criticalIds);
        foreach (var taskId in criticalIdSet)
        {
            var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null && task.Status != TimeLeaf.Models.Enums.TaskStatus.Completed)
            {
                risks.Add(
                    new ProjectRisk(
                        RiskType.ScheduleDelay,
                        $"タスク「{task.Name}」はクリティカルパス上にあり、遅延がプロジェクト全体に波及します。",
                        task.Id
                    )
                );
            }
        }

        return risks;
    }
}
