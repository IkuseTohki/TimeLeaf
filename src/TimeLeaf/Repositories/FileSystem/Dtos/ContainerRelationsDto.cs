using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// コンテナの関連情報を保持するためのDTO。
/// </summary>
internal record ContainerRelationsDto(Guid Id, List<Guid> WatcherIds, List<Guid> RelatedTaskIds);
