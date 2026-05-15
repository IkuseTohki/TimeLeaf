using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクを別のコンテナ（親タスク）へ移動し、永続化するユースケースのインターフェース。
/// </summary>
public interface IMoveTaskUseCase
{
    /// <summary>
    /// タスクを指定されたコンテナへ移動します。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <param name="taskId">移動するタスクのID。</param>
    /// <param name="newParentId">移動先のコンテナID（nullの場合は未分類）。</param>
    /// <returns>非同期タスク。</returns>
    Task ExecuteAsync(Project project, Guid taskId, Guid? newParentId);
}
