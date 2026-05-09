using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// プロジェクト内のアイテム（タスク・コンテナ）の表示順序（IDリスト）を保持するためのDTO。
/// </summary>
internal record ProjectSortOrderDto(List<Guid> OrderedIds);
