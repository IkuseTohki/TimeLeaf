using System;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// プロジェクトのメタデータを保持するためのDTO（.project ファイル用）。
/// </summary>
internal record ProjectMetadataDto(
    Guid ProjectId,
    DateTime CreatedAt,
    string CreatedBy,
    int SchemaVersion,
    bool IsArchived = false,
    DateTime? LockedUntil = null
);
