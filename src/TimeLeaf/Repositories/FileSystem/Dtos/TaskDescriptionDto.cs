using System;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// タスクの説明文を保持するためのDTO。
/// </summary>
internal record TaskDescriptionDto(Guid Id, string Description);
