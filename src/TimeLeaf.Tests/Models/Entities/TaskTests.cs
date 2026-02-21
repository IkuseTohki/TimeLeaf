using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class TaskTests
{
    /// <summary>
    /// テスト観点: Description プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Description_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new Task();
        var description = "This is a test task description.";

        // Act
        task.Description = description;

        // Assert
        Assert.AreEqual(description, task.Description);
    }
}
