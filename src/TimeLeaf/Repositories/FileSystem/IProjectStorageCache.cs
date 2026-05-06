using System;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// ストレージ層のキャッシュ管理を抽象化するインターフェース。
/// キャッシュキーの生成ロジックをカプセル化し、ドメイン識別子によるアクセスを提供します。
/// </summary>
public interface IProjectStorageCache
{
    /// <summary>
    /// 一般カテゴリのキャッシュ値を取得します。
    /// </summary>
    bool TryGetCategory(Guid entityId, string category, out string? json);

    /// <summary>
    /// 一般カテゴリのキャッシュ値を更新します。
    /// </summary>
    void UpdateCategory(Guid entityId, string category, string json);

    /// <summary>
    /// コメントのキャッシュ値を取得します。
    /// </summary>
    bool TryGetComment(Guid taskId, Guid commentId, out string? json);

    /// <summary>
    /// コメントのキャッシュ値を更新します。
    /// </summary>
    void UpdateComment(Guid taskId, Guid commentId, string json);

    /// <summary>
    /// キャッシュされている項目の総数を取得します。
    /// </summary>
    int Count { get; }
}
