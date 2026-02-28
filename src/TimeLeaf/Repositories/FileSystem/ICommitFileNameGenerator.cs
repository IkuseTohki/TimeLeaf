using System;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 変更履歴ファイル（コミット）の命名規則を扱うインターフェース。
/// </summary>
public interface ICommitFileNameGenerator
{
    /// <summary>
    /// 各要素からファイル名を生成します。
    /// </summary>
    string Generate(DateTime timestamp, string userId, Guid guid, string category);

    /// <summary>
    /// ファイル名から要素を解析します。
    /// </summary>
    CommitFileName Parse(string fileName);
}
