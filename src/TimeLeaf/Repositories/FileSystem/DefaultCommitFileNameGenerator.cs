using System;
using System.Globalization;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 変更履歴ファイル（コミット）の命名規則の標準的な実装。
/// 規則: {yyyyMMdd}_{HHmmss}_{fff}_{UserID}_{Category}.json
/// </summary>
public class DefaultCommitFileNameGenerator : ICommitFileNameGenerator
{
    private const string TimeFormat = "yyyyMMdd_HHmmss_fff";

    public string Generate(DateTime timestamp, string userId, string category, Guid? entityId = null)
    {
        if (category == "Deleted" && entityId.HasValue)
        {
            return $"{timestamp.ToString(TimeFormat)}_{userId}_{entityId}_{category}.json";
        }
        return $"{timestamp.ToString(TimeFormat)}_{userId}_{category}.json";
    }

    public CommitFileName Parse(string fileName)
    {
        var nameWithoutExt = fileName.Replace(".json", "");
        var parts = nameWithoutExt.Split('_');

        if (parts.Length < 5)
        {
            throw new ArgumentException($"Invalid file name format: {fileName}");
        }

        // タイムスタンプのパース (yyyyMMdd_HHmmss_fff)
        var timeStr = $"{parts[0]}_{parts[1]}_{parts[2]}";
        // ファイル名はLocal(JST)として保存されているため、Localとしてパースする
        var timestamp = DateTime.ParseExact(
            timeStr,
            TimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal
        );

        var userId = parts[3];

        // カテゴリ名にGUIDが含まれているか確認（Deletedの場合）
        string category;
        if (parts.Length >= 6 && parts[parts.Length - 1] == "Deleted")
        {
            category = parts[parts.Length - 1];
        }
        else
        {
            category = string.Join("_", parts, 4, parts.Length - 4);
        }

        return new CommitFileName(timestamp, userId, category);
    }
}
