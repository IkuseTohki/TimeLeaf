using System.ComponentModel.DataAnnotations;

namespace TimeLeaf.Models.Enums;

/// <summary>
/// タスクの優先度。
/// </summary>
public enum TaskPriority
{
    /// <summary>高。</summary>
    [Display(Name = "高")]
    High,

    /// <summary>中（デフォルト）。</summary>
    [Display(Name = "中")]
    Medium,

    /// <summary>低。</summary>
    [Display(Name = "低")]
    Low,
}
