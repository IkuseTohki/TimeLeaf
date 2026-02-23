using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトの節目となるマイルストーン。
/// </summary>
public class Milestone
{
    /// <summary>
    /// マイルストーンの日付。
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// マイルストーンのラベル。
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
