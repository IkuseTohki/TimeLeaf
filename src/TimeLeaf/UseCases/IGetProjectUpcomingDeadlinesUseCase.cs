using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクト内の期限が近いタスクを取得するためのユースケースインターフェース。
/// </summary>
public interface IGetProjectUpcomingDeadlinesUseCase
{
    /// <summary>
    /// 指定されたプロジェクトから、期限が指定された日数以内の未完了タスクを取得します。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <param name="days">期限までの日数（期間）。</param>
    /// <returns>期限が近いタスクのリスト。</returns>
    IEnumerable<ProjectTask> Execute(Project project, int days);
}
