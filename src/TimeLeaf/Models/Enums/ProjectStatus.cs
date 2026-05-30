using System.ComponentModel.DataAnnotations;

namespace TimeLeaf.Models.Enums;

/// <summary>
/// プロジェクトのライフサイクル状態。
/// </summary>
public enum ProjectStatus
{
    /// <summary>準備中。</summary>
    [Display(Name = "準備中")]
    Initial,

    /// <summary>進行中。</summary>
    [Display(Name = "進行中")]
    InProgress,

    /// <summary>完了。</summary>
    [Display(Name = "完了")]
    Completed,

    /// <summary>一時停止。</summary>
    [Display(Name = "一時停止")]
    Suspended,
}
