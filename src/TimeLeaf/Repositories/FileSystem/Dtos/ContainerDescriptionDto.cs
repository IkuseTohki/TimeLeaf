using System;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// コンテナの詳細説明を保持するためのDTO。
/// </summary>
internal record ContainerDescriptionDto(Guid Id, string Description);
