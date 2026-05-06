using System;
using System.Collections.Concurrent;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// ストレージ層のキャッシュ管理を具現化するクラス。
/// キー生成ロジックを内部に隠蔽（カプセル化）します。
/// </summary>
public class ProjectStorageCache : IProjectStorageCache
{
    private readonly ConcurrentDictionary<string, string> _cache = new();

    public int Count => _cache.Count;

    public bool TryGetCategory(Guid entityId, string category, out string? json)
    {
        var key = GetCategoryKey(entityId, category);
        return _cache.TryGetValue(key, out json);
    }

    public void UpdateCategory(Guid entityId, string category, string json)
    {
        var key = GetCategoryKey(entityId, category);
        _cache[key] = json;
    }

    public bool TryGetComment(Guid taskId, Guid commentId, out string? json)
    {
        var key = GetCommentKey(taskId, commentId);
        return _cache.TryGetValue(key, out json);
    }

    public void UpdateComment(Guid taskId, Guid commentId, string json)
    {
        var key = GetCommentKey(taskId, commentId);
        _cache[key] = json;
    }

    private string GetCategoryKey(Guid entityId, string category) => $"{entityId}_{category}";

    private string GetCommentKey(Guid taskId, Guid commentId) => $"{taskId}_Comment_{commentId}";
}
