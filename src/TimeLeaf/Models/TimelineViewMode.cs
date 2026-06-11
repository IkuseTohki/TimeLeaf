namespace TimeLeaf.Models
{
    /// <summary>
    /// タイムラインの表示モード。
    /// </summary>
    public enum TimelineViewMode
    {
        /// <summary>
        /// 予定のみを表示。
        /// </summary>
        PlanOnly,

        /// <summary>
        /// 実績のみを表示。
        /// </summary>
        ActualOnly,

        /// <summary>
        /// 予定と実績の両方を表示。
        /// </summary>
        Dual,
    }
}
