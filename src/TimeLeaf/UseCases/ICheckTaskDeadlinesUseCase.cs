using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクの期限をチェックし、期限切れのタスクについて通知を発行するユースケース。
/// </summary>
public interface ICheckTaskDeadlinesUseCase
{
    /// <summary>
    /// 指定されたプロジェクト群のタスク期限をチェックし、必要に応じて通知を発行します。
    /// </summary>
    /// <param name="projects">チェック対象のプロジェクトリスト。</param>
    void Execute(IEnumerable<Project> projects);
}
