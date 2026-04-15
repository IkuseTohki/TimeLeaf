using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいタスクを追加し、永続化するユースケースのインターフェース。
/// </summary>
public interface IAddTaskUseCase
{
    Task ExecuteAsync(
        Project project,
        string name,
        string description,
        TimeLeaf.Models.Enums.TaskStatus status,
        TaskPriority priority,
        DateTime? scheduledStartDate,
        DateTime? deadline,
        DateTime? actualStartDate,
        DateTime? actualEndDate,
        double estimatedCost,
        double actualCost,
        string assignee
    );
}
