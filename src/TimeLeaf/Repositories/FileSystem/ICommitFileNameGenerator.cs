using System;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 変更履歴ファイル（コミット）の命名規則を扱うインターフェース。
/// 規則: {yyyyMMdd}_{HHmmss}_{fff}_{UserID}_{Category}.json
/// </summary>
public interface ICommitFileNameGenerator
{
    /// <summary>
    /// 各要素からファイル名を生成します。
    /// </summary>
    string Generate(DateTime timestamp, string userId, string category, Guid? entityId = null);

    /// <summary>
    /// ファイル名から要素を解析します。
    /// </summary>
    CommitFileName Parse(string fileName);
}
