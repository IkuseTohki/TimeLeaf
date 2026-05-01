using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Tests.Models.Entities;

[TestClass]
public class ProjectAssignmentTests
{
    /// <summary>
    /// テスト観点: ユーザーをプロジェクトにアサインし、リストに含まれることを確認する。
    /// </summary>
    [TestMethod]
    public void AssignUser_ShouldAddUserToAssignedUsers()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var userId = Guid.NewGuid();

        // Act
        project.AssignUser(userId);

        // Assert
        Assert.IsTrue(project.AssignedUserIds.Contains(userId));
    }

    /// <summary>
    /// テスト観点: 同一ユーザーを複数回アサインしても、リストが重複しないことを確認する。
    /// </summary>
    [TestMethod]
    public void AssignUser_ShouldNotDuplicateUsers()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var userId = Guid.NewGuid();

        // Act
        project.AssignUser(userId);
        project.AssignUser(userId);

        // Assert
        Assert.AreEqual(1, project.AssignedUserIds.Count);
    }

    /// <summary>
    /// テスト観点: ユーザーのアサインを解除できることを確認する。
    /// </summary>
    [TestMethod]
    public void UnassignUser_ShouldRemoveUserFromAssignedUsers()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var userId = Guid.NewGuid();
        project.AssignUser(userId);

        // Act
        project.UnassignUser(userId);

        // Assert
        Assert.IsFalse(project.AssignedUserIds.Contains(userId));
    }
}
