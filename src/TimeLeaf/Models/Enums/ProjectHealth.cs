namespace TimeLeaf.Models.Enums;

/// <summary>
/// プロジェクトの健全性ステータス。
/// </summary>
public enum ProjectHealth
{
    /// <summary>順調。</summary>
    Healthy,
    /// <summary>注意が必要。</summary>
    Warning,
    /// <summary>遅延・危険。</summary>
    Delayed
}
