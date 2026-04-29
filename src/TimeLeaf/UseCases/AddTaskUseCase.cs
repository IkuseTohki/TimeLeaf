using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいタスクを追加し、永続化するユースケース。
/// </summary>
public class AddTaskUseCase : IAddTaskUseCase
{
    private readonly ISaveProjectUseCase _saveUseCase;

    public AddTaskUseCase(ISaveProjectUseCase saveUseCase)
    {
        _saveUseCase = saveUseCase;
    }

    public async Task ExecuteAsync(
        Project project,
        string name,
        string description,
        TimeLeaf.Models.Enums.TaskStatus status,
        TaskPriority priority,
        Guid? parentId,
        DateTime? scheduledStartDate,
        DateTime? deadline,
        DateTime? actualStartDate,
        DateTime? actualEndDate,
        double estimatedCost,
        double actualCost,
        string assignee
    )
    {
        var task = new ProjectTask();
        task.UpdateName(name);
        task.UpdateDescription(description);
        task.UpdateStatus(status);
        task.UpdatePriority(priority);
        task.SetParentId(parentId);
        task.UpdateSchedule(scheduledStartDate, deadline);
        task.UpdateActualDates(actualStartDate, actualEndDate);
        task.UpdateEstimatedCost(estimatedCost);
        task.UpdateActualCost(actualCost);
        task.AssignTo(assignee);

        project.AddTask(task);

        await _saveUseCase.ExecuteAsync(project);
    }
}
