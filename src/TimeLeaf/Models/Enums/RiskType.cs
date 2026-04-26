namespace TimeLeaf.Models.Enums;

/// <summary>
/// プロジェクト・リスクの種類。
/// </summary>
public enum RiskType
{
    /// <summary>循環参照。</summary>
    CycleDetected,

    /// <summary>制約違反（先行未了なのに開始など）。</summary>
    ConstraintViolation,

    /// <summary>遅延リスク（クリティカルパス上の遅延）。</summary>
    ScheduleDelay,

    /// <summary>期限切れ。</summary>
    Overdue,
}
