using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class ProjectStorageCacheTests
{
    /// <summary>
    /// テスト観点: カテゴリキャッシュの保存と取得が、キーの知識を意識せずに正しく行えるか確認する。
    /// </summary>
    [TestMethod]
    public void UpdateAndGetCategory_ShouldWorkCorrectly()
    {
        // Arrange
        IProjectStorageCache cache = new ProjectStorageCache();
        var entityId = Guid.NewGuid();
        var category = "Project_Basic";
        var json = "{\"Name\":\"Test\"}";

        // Act
        cache.UpdateCategory(entityId, category, json);
        bool found = cache.TryGetCategory(entityId, category, out var result);

        // Assert
        Assert.IsTrue(found);
        Assert.AreEqual(json, result);
    }

    /// <summary>
    /// テスト観点: コメントキャッシュの保存と取得が、TaskIdとCommentIdのペアで正しく行えるか確認する。
    /// </summary>
    [TestMethod]
    public void UpdateAndGetComment_ShouldWorkCorrectly()
    {
        // Arrange
        IProjectStorageCache cache = new ProjectStorageCache();
        var taskId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var json = "{\"Content\":\"Hello\"}";

        // Act
        cache.UpdateComment(taskId, commentId, json);
        bool found = cache.TryGetComment(taskId, commentId, out var result);

        // Assert
        Assert.IsTrue(found);
        Assert.AreEqual(json, result);
    }

    /// <summary>
    /// テスト観点: 異なるTaskId/CommentIdの組み合わせでキャッシュが衝突しないことを確認する。
    /// </summary>
    [TestMethod]
    public void Cache_ShouldNotConflictBetweenDifferentEntities()
    {
        // Arrange
        IProjectStorageCache cache = new ProjectStorageCache();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var category = "Task_Planning";

        // Act
        cache.UpdateCategory(id1, category, "json1");
        cache.UpdateCategory(id2, category, "json2");

        // Assert
        cache.TryGetCategory(id1, category, out var res1);
        cache.TryGetCategory(id2, category, out var res2);
        Assert.AreEqual("json1", res1);
        Assert.AreEqual("json2", res2);
    }
}
