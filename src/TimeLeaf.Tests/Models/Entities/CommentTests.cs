using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class CommentTests
{
    private static readonly Guid ValidTaskId = Guid.NewGuid();
    private static readonly Guid ValidAuthorId = Guid.NewGuid();
    private const string ValidContent = "テストコメント";

    [TestMethod]
    public void Constructor_ShouldThrow_WhenTaskIdIsEmpty()
    {
        // テスト観点: TaskId が Empty の場合、ArgumentException がスローされることを確認する。
        try
        {
            _ = new Comment
            {
                TaskId = Guid.Empty,
                AuthorId = ValidAuthorId,
                Content = ValidContent,
            };
            Assert.Fail("例外がスローされるべきです");
        }
        catch (ArgumentException) { }
    }

    [TestMethod]
    public void Constructor_ShouldThrow_WhenAuthorIdIsEmpty()
    {
        // テスト観点: AuthorId が Empty の場合、ArgumentException がスローされることを確認する。
        try
        {
            _ = new Comment
            {
                TaskId = ValidTaskId,
                AuthorId = Guid.Empty,
                Content = ValidContent,
            };
            Assert.Fail("例外がスローされるべきです");
        }
        catch (ArgumentException) { }
    }

    [TestMethod]
    public void Constructor_ShouldThrow_WhenContentIsEmpty()
    {
        // テスト観点: Content が 空文字の場合、ArgumentException がスローされることを確認する。
        try
        {
            _ = new Comment
            {
                TaskId = ValidTaskId,
                AuthorId = ValidAuthorId,
                Content = string.Empty,
            };
            Assert.Fail("例外がスローされるべきです");
        }
        catch (ArgumentException) { }
    }
}
