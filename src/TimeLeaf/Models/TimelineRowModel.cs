using System;

namespace TimeLeaf.Models
{
    /// <summary>
    /// タイムライン（ガントチャート）の1行分の表示データを保持するモデル。
    /// </summary>
    public class TimelineRowModel
    {
        public Guid TaskId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Depth { get; set; }
        public bool IsContainer { get; set; }

        // 予定 (Plan)
        public DateTime? PlannedStart { get; set; }
        public DateTime? PlannedEnd { get; set; }

        // 実績 (Actual)
        public DateTime? ActualStart { get; set; }
        public DateTime? ActualEnd { get; set; }

        // 進捗
        public int ProgressPercentage { get; set; }

        // 担当者情報
        public string ThemeColor { get; set; } = "Transparent";
        public string DisplayName { get; set; } = string.Empty;
        public string? IconPath { get; set; }

        // 稲妻線用: 今日時点での進捗偏差 (日単位)
        // プラスは先行、マイナスは遅延
        public double ProgressOffsetDays { get; set; }
    }
}
