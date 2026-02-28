using System;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// 変更履歴ファイル（コミット）の解析済みデータを保持するレコード。
/// </summary>
public record CommitFileName(DateTime Timestamp, string UserId, Guid Guid, string Category);
