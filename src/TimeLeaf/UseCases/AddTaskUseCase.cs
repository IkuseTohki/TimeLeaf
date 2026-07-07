using System;
using System.Linq;
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
        Guid? assignee
    )
    {
        var task = new ProjectTask();
        task.UpdateName(name);
        task.UpdateDescription(description);
        task.UpdateStatus(status);
        task.UpdatePriority(priority);
        task.UpdateSchedule(scheduledStartDate, deadline);
        task.UpdateActualDates(actualStartDate, actualEndDate);
        task.UpdateEstimatedCost(estimatedCost);
        task.UpdateActualCost(actualCost);
        task.AssignTo(assignee);

        project.AddTask(task);

        // parentId が指定されている場合はコンテナの Children リストにも登録する。
        // SetParentId だけでは task.ParentId が設定されるが、
        // コンテナ側の Children リストには追加されないため、
        // RebuildContainers が正しく機能しない。
        if (parentId.HasValue)
        {
            var container = project.Containers.FirstOrDefault(c => c.Id == parentId.Value);
            if (container != null)
            {
                container.AddChild(task); // 内部で SetParentId も呼ばれる
            }
            else
            {
                // コンテナが見つからない場合は ParentId のみ設定
                task.SetParentId(parentId);
            }
        }

        await _saveUseCase.ExecuteAsync(project);
    }
}
