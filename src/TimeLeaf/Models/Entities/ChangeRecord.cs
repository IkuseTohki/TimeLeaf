using System;

namespace TimeLeaf.Models.Entities;

/// <summary>
/// タスクまたはプロジェクトに対する個別の変更記録（コミット）を表すレコード。
/// </summary>
/// <param name="Timestamp">変更が記録された日時。</param>
/// <param name="UserId">変更を行ったユーザーのID。</param>
/// <param name="Category">変更されたデータのカテゴリ（StorageCategoriesに対応）。</param>
public record ChangeRecord(DateTime Timestamp, Guid UserId, string Category, string? Content = null);
