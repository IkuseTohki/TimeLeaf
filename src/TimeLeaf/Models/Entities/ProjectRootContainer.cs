using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクト直下のコンテナとタスクを集約し、ルートレベルでの並び替えを可能にするためのラッパークラス。
/// </summary>
public class ProjectRootContainer : IWorkItemContainer
{
    private readonly Project _project;

    public ProjectRootContainer(Project project)
    {
        _project = project ?? throw new ArgumentNullException(nameof(project));
    }

    /// <summary>
    /// プロジェクト直下のコンテナと、親を持たないタスクを統合して返す。
    /// </summary>
    /// <summary>
    /// プロジェクト直下のコンテナと、親を持たないタスクを統合し、定義された順序で返す。
    /// </summary>
    public IEnumerable<ProjectWorkItem> Children
    {
        get
        {
            var rootItems = _project
                .Containers.Cast<ProjectWorkItem>()
                .Concat(_project.Tasks.Where(t => t.ParentId == null).Cast<ProjectWorkItem>())
                .ToList();

            var order = _project.RootWorkItemOrder;
            if (order == null || !order.Any())
                return rootItems.OrderBy(i => i.CreatedAt);

            // 定義された順序に従って並べ替え
            var ordered = order
                .Select(id => rootItems.FirstOrDefault(i => i.Id == id))
                .Where(i => i != null)
                .Select(i => i!)
                .ToList();

            // 順序リストに含まれていないアイテム（新規追加等）を末尾に追加
            var missing = rootItems.Where(i => !order.Contains(i.Id)).OrderBy(i => i.CreatedAt);
            ordered.AddRange(missing);

            return ordered;
        }
    }

    public void MoveChild(Guid childId, int newIndex)
    {
        // プロジェクト直下のアイテムの移動ロジック
        var currentItems = Children.ToList();
        var item = currentItems.FirstOrDefault(i => i.Id == childId);
        if (item == null)
            return;

        int oldIndex = currentItems.IndexOf(item);
        if (oldIndex == -1 || oldIndex == newIndex)
            return;

        if (newIndex < 0 || newIndex >= currentItems.Count)
            return;

        currentItems.RemoveAt(oldIndex);
        currentItems.Insert(newIndex, item);

        // ドメインモデル側の順序を更新
        _project.ReorderWorkItems(currentItems.Select(i => i.Id));
    }
}
