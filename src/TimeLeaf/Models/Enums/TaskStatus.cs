using System.ComponentModel.DataAnnotations;

namespace TimeLeaf.Models.Enums;

/// <summary>
/// タスクの進捗状態。
/// </summary>
public enum TaskStatus
{
    /// <summary>未着手。</summary>
    [Display(Name = "未着手")]
    NotStarted,

    /// <summary>着手中。</summary>
    [Display(Name = "着手中")]
    InProgress,

    /// <summary>レビュー待ち。</summary>
    [Display(Name = "レビュー待ち")]
    InReview,

    /// <summary>完了。</summary>
    [Display(Name = "完了")]
    Completed,
}
