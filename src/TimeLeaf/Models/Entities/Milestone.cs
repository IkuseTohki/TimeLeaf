using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// プロジェクトの節目となるマイルストーン。
/// </summary>
public record Milestone
{
    /// <summary>
    /// マイルストーンの日付。
    /// </summary>
    public DateTime Date { get; init; }

    private readonly string _label = string.Empty;

    /// <summary>
    /// マイルストーンのラベル。
    /// </summary>
    public string Label
    {
        get => _label;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Milestone label cannot be empty.", nameof(value));
            _label = value;
        }
    }

    /// <summary>
    /// デフォルトコンストラクタ（シリアライズ用）。
    /// </summary>
    public Milestone() { }

    /// <summary>
    /// パラメータ付きコンストラクタ。
    /// </summary>
    /// <param name="date">日付。</param>
    /// <param name="label">ラベル。</param>
    [System.Text.Json.Serialization.JsonConstructor]
    public Milestone(DateTime date, string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Milestone label cannot be empty.", nameof(label));

        Date = date;
        Label = label;
    }
}
