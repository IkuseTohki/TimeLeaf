namespace TimeLeaf.ViewModels.Workspace;

using TimeLeaf.Models.Entities;

/// <summary>
/// タスクまたはコンテナを階層構造で表示するための表示用モデル。
/// </summary>
public record ProjectTaskDisplayItem(ProjectWorkItem Item, int Depth, bool IsContainer);
