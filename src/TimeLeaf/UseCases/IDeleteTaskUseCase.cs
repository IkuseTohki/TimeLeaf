using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトからタスクを削除するユースケースのインターフェース。
/// </summary>
public interface IDeleteTaskUseCase
{
    /// <summary>
    /// 指定されたタスクをプロジェクトから削除します。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <param name="taskId">削除するタスクのID。</param>
    /// <returns>非同期タスク。</returns>
    Task ExecuteAsync(Project project, Guid taskId);
}
