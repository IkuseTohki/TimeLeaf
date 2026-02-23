using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectTaskStep15Tests
{
    /// <summary>
    /// テスト観点: Assignee プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Assignee_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var assignee = "user1";

        // Act
        task.Assignee = assignee;

        // Assert
        Assert.AreEqual(assignee, task.Assignee);
    }

    /// <summary>
    /// テスト観点: Dependencies プロパティが正常に読み書きできることを確認する。
    /// </summary>
    [TestMethod]
    public void Dependencies_ShouldBeReadAndWrite()
    {
        // Arrange
        var task = new ProjectTask();
        var dependencyId = Guid.NewGuid();

        // Act
        task.Dependencies.Add(dependencyId);

        // Assert
        Assert.Contains(dependencyId, task.Dependencies);
    }
}
