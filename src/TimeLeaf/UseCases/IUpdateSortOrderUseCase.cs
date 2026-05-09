using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクト内のアイテム（タスク・コンテナ）の表示順序を一括更新し、永続化するユースケースのインターフェース。
/// </summary>
public interface IUpdateSortOrderUseCase
{
    /// <summary>
    /// 指定された ID リストの順序に基づいて、アイテムの表示順序を更新し保存します。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <param name="orderedIds">期待される順序で並んだアイテムIDのリスト。</param>
    Task ExecuteAsync(Project project, IEnumerable<Guid> orderedIds);
}
