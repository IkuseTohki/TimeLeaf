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

    public string Generate(DateTime timestamp, string userId, string category)
    {
        // 常に渡された日時のまま（JST前提）文字列化する。
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
        // カテゴリは残りのパーツすべて（カテゴリ名に _ が含まれる可能性があるため）
        var category = string.Join("_", parts, 4, parts.Length - 4);

        return new CommitFileName(timestamp, userId, category);
    }
}
