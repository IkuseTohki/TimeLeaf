using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクト内のアイテム（タスク・コンテナ）の表示順序を一括更新し、永続化するユースケース。
/// </summary>
public class UpdateSortOrderUseCase : IUpdateSortOrderUseCase
{
    private readonly ISaveProjectUseCase _saveUseCase;

    public UpdateSortOrderUseCase(ISaveProjectUseCase saveUseCase)
    {
        _saveUseCase = saveUseCase;
    }

    public async Task ExecuteAsync(Project project, IEnumerable<Guid> orderedIds)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));
        if (orderedIds == null)
            throw new ArgumentNullException(nameof(orderedIds));

        // ドメインモデルに対して物理的な並べ替えを指示
        project.ReorderWorkItems(orderedIds);

        // 変更を保存（Project_SortOrder カテゴリとして保存される）
        await _saveUseCase.ExecuteAsync(project);
    }
}
