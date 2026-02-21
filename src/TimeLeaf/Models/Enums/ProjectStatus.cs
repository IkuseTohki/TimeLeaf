namespace TimeLeaf.Models.Enums;

/// <summary>
/// プロジェクトのライフサイクル状態。
/// </summary>
public enum ProjectStatus
{
    /// <summary>準備中。</summary>
    Initial,
    /// <summary>進行中。</summary>
    InProgress,
    /// <summary>完了。</summary>
    Completed,
    /// <summary>一時停止。</summary>
    Suspended
}
