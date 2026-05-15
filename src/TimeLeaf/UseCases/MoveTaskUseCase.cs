using System;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクを別のコンテナ（親タスク）へ移動し、永続化するユースケース。
/// </summary>
public class MoveTaskUseCase : IMoveTaskUseCase
{
    private readonly ISaveProjectUseCase _saveProjectUseCase;

    public MoveTaskUseCase(ISaveProjectUseCase saveProjectUseCase)
    {
        _saveProjectUseCase = saveProjectUseCase;
    }

    public async Task ExecuteAsync(Project project, Guid taskId, Guid? newParentId)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        var task = project.Tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
        {
            throw new InvalidOperationException($"Task with ID {taskId} not found in project.");
        }

        // コンテナの存在確認（nullでない場合）
        if (newParentId.HasValue)
        {
            var containerExists = project.Containers.Any(c => c.Id == newParentId.Value);
            if (!containerExists)
            {
                throw new InvalidOperationException($"Container with ID {newParentId.Value} not found in project.");
            }
        }

        // 親IDの更新
        task.SetParentId(newParentId);

        // プロジェクトの保存
        await _saveProjectUseCase.ExecuteAsync(project);
    }
}
