using System;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// プロジェクトの基本情報を保持するためのDTO（スナップショット用）。
/// </summary>
internal record ProjectBasicDto(string Name, ProjectStatus Status);
