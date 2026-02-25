using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class CommentTests
{
    /// <summary>
    /// テスト観点: 必須項目（TaskId, AuthorId, Content）が欠けている場合、例外をスローすること。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldThrow_ForMissingRequiredFields()
    {
        var taskId = Guid.NewGuid();
        var authorId = "user1";
        var content = "テストコメント";

        // TaskId が Empty
        try
        {
            _ = new Comment { TaskId = Guid.Empty, AuthorId = authorId, Content = content };
            Assert.Fail("TaskId が Empty の場合に例外をスローすべき");
        }
        catch (ArgumentException) { }

        // AuthorId が 空
        try
        {
            _ = new Comment { TaskId = taskId, AuthorId = "", Content = content };
            Assert.Fail("AuthorId が 空 の場合に例外をスローすべき");
        }
        catch (ArgumentException) { }

        // Content が 空
        try
        {
            _ = new Comment { TaskId = taskId, AuthorId = authorId, Content = "" };
            Assert.Fail("Content が 空 の場合に例外をスローすべき");
        }
        catch (ArgumentException) { }
    }
}
