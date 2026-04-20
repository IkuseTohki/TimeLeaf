namespace TimeLeaf.Models.Enums;

/// <summary>
/// 全タスク一覧の表示モードを定義します。
/// </summary>
public enum AllTasksViewMode
{
    /// <summary>
    /// 高密度な表形式。
    /// </summary>
    List,

    /// <summary>
    /// 日付ごとの垂直タイムライン形式。
    /// </summary>
    Timeline,

    /// <summary>
    /// プロジェクトごとのカード（グリッド）形式。
    /// </summary>
    Grid,
}
