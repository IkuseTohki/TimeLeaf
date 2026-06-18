using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクの変更履歴を取得するユースケースのインターフェース。
/// </summary>
public interface IGetTaskHistoryUseCase
{
    /// <summary>
    /// 指定されたタスクの変更履歴を非同期で取得します。
    /// </summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <param name="taskId">タスクID。</param>
    /// <returns>変更記録のリスト。</returns>
    Task<IEnumerable<ChangeRecord>> ExecuteAsync(Guid projectId, Guid taskId);
}
