using System;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// マイルストーン情報を保持するためのDTO。
/// </summary>
internal record MilestoneDto(DateTime Date, string Label);
