namespace TimeLeaf.Models.Enums;

/// <summary>
/// タスクの進捗状態。
/// </summary>
public enum TaskStatus
{
    /// <summary>未着手。</summary>
    NotStarted,
    /// <summary>着手中。</summary>
    InProgress,
    /// <summary>レビュー待ち。</summary>
    InReview,
    /// <summary>完了。</summary>
    Completed
}
