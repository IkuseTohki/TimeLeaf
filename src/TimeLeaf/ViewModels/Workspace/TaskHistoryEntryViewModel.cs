using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Repositories.FileSystem.Converters;
using TimeLeaf.Repositories.FileSystem.Dtos;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// 単一の変更履歴エントリを表示するためのViewModel。
/// </summary>
public class TaskHistoryEntryViewModel : ObservableObject
{
    /// <summary>
    /// 変更日時。
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// 変更を行ったユーザー名。
    /// </summary>
    public string UserName { get; }

    /// <summary>
    /// 変更内容の表示名。
    /// </summary>
    public string ActionDisplayName { get; }

    /// <summary>
    /// 具体的な変更内容の要約。
    /// </summary>
    public string? DetailSummary { get; }

    /// <summary>
    /// 詳細サマリーが存在するかどうか。
    /// </summary>
    public bool HasDetailSummary => !string.IsNullOrWhiteSpace(DetailSummary);

    public TaskHistoryEntryViewModel(ChangeRecord record, IUserService userService)
    {
        Timestamp = record.Timestamp;
        UserName = userService.GetUserName(record.UserId.ToString());
        ActionDisplayName = GetActionDisplayName(record.Category);
        DetailSummary = GetDetailSummary(record);
    }

    private string? GetDetailSummary(ChangeRecord record)
    {
        if (string.IsNullOrEmpty(record.Content))
            return null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(), new FlexibleNullableGuidConverter() },
            };

            var summary = record.Category switch
            {
                StorageCategories.TaskPlanning => ParsePlanning(record.Content, options),
                StorageCategories.TaskProgress => ParseProgress(record.Content, options),
                StorageCategories.TaskDescription => "（説明文が更新されました）",
                _ => null,
            };

            return string.IsNullOrWhiteSpace(summary) ? null : summary;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"History Detail Parse Error: {ex.Message}");
            return null;
        }
    }

    private string ParsePlanning(string json, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<TaskPlanningDto>(json, options);
        if (dto == null)
            return string.Empty;

        var details = new System.Collections.Generic.List<string>();
        if (!string.IsNullOrEmpty(dto.Name))
            details.Add($"名称: {dto.Name}");
        if (dto.ScheduledStartDate.HasValue)
            details.Add($"開始予定: {dto.ScheduledStartDate.Value:MM/dd}");
        if (dto.Deadline.HasValue)
            details.Add($"期限: {dto.Deadline.Value:MM/dd}");
        if (dto.Priority != default)
            details.Add($"優先度: {dto.Priority}");
        if (dto.EstimatedCost > 0)
            details.Add($"見積工数: {dto.EstimatedCost}h");

        return string.Join(", ", details);
    }

    private string ParseProgress(string json, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<TaskProgressDto>(json, options);
        if (dto == null)
            return string.Empty;

        var details = new System.Collections.Generic.List<string>();
        details.Add($"ステータス: {dto.Status}");
        if (dto.ActualCost > 0)
            details.Add($"実績工数: {dto.ActualCost}h");

        return string.Join(", ", details);
    }

    private string GetActionDisplayName(string category)
    {
        return category switch
        {
            StorageCategories.TaskPlanning => "計画（名称・期限等）を更新しました",
            StorageCategories.TaskProgress => "進捗（ステータス・工数等）を更新しました",
            StorageCategories.TaskDescription => "詳細説明を編集しました",
            StorageCategories.Comment => "コメントを投稿しました",
            StorageCategories.Deleted => "タスクを削除しました",
            _ => $"変更履歴を記録しました ({category})",
        };
    }
}
