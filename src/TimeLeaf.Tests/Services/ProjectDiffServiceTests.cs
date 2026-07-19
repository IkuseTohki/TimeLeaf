using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Services;

[TestClass]
public class ProjectDiffServiceTests
{
    [TestMethod]
    public void CalculateDiff_ShouldDetectNewAssignment()
    {
        // テスト観点: 他の誰かが担当していたタスクが、同期後に自分（myUserId）にアサイン変更されたことを検知する。

        // Arrange
        var myUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var oldProject = new Project(Guid.NewGuid());
        var task = new ProjectTask();
        task.UpdateName("Task1");
        task.AssignTo(otherUserId);
        oldProject.AddTask(task);

        // Project は直接的なコピー機能がないため、デシリアライズ用コンストラクタを利用してクローンを生成
        var newProject = new Project(
            oldProject.Id,
            oldProject.Name,
            oldProject.Description,
            oldProject.Status,
            oldProject.CreatedAt,
            oldProject.UpdatedAt,
            oldProject.CreatedBy,
            oldProject.Tasks.Select(t => t.Clone()).ToList(),
            oldProject.Containers.ToList(),
            oldProject.Milestones.ToList(),
            oldProject.AssignedUserIds.ToList()
        );
        newProject.Tasks.First().AssignTo(myUserId);

        var service = new ProjectDiffService();

        // Act
        var diff = service.CalculateDiff(oldProject, newProject, myUserId);

        // Assert
        Assert.IsTrue(diff.AssignedTaskIds.Contains(task.Id), "タスクが自分にアサインされたことが検知されていません。");
    }

    [TestMethod]
    public void CalculateDiff_ShouldDetectNewComments()
    {
        // テスト観点: 同期後にタスクに新しいコメントが増えていることを検知する。

        // Arrange
        var myUserId = Guid.NewGuid();

        var oldProject = new Project(Guid.NewGuid());
        var task = new ProjectTask();
        task.UpdateName("Task1");
        oldProject.AddTask(task);

        // Project は直接的なコピー機能がないため、デシリアライズ用コンストラクタを利用してクローンを生成
        var newProject = new Project(
            oldProject.Id,
            oldProject.Name,
            oldProject.Description,
            oldProject.Status,
            oldProject.CreatedAt,
            oldProject.UpdatedAt,
            oldProject.CreatedBy,
            oldProject.Tasks.Select(t => t.Clone()).ToList(),
            oldProject.Containers.ToList(),
            oldProject.Milestones.ToList(),
            oldProject.AssignedUserIds.ToList()
        );

        var newComment = new Comment(
            Guid.NewGuid(),
            newProject.Tasks.First().Id,
            Guid.NewGuid(),
            DateTime.Now,
            "New comment",
            null
        );
        newProject.Tasks.First().AddComment(newComment);

        var service = new ProjectDiffService();

        // Act
        var diff = service.CalculateDiff(oldProject, newProject, myUserId);

        // Assert
        Assert.IsTrue(diff.NewCommentTaskIds.Contains(task.Id), "新しいコメントが追加されたことが検知されていません。");
    }

    [TestMethod]
    public void CalculateDiff_ShouldIgnoreOwnComments()
    {
        // テスト観点: 同期後に増えたコメントの作成者が自分自身（myUserId）の場合、検知対象から除外すること。

        // Arrange
        var myUserId = Guid.NewGuid();

        var oldProject = new Project(Guid.NewGuid());
        var task = new ProjectTask();
        task.UpdateName("Task1");
        oldProject.AddTask(task);

        var newProject = new Project(
            oldProject.Id,
            oldProject.Name,
            oldProject.Description,
            oldProject.Status,
            oldProject.CreatedAt,
            oldProject.UpdatedAt,
            oldProject.CreatedBy,
            oldProject.Tasks.Select(t => t.Clone()).ToList(),
            oldProject.Containers.ToList(),
            oldProject.Milestones.ToList(),
            oldProject.AssignedUserIds.ToList()
        );

        var newComment = new Comment(
            Guid.NewGuid(),
            newProject.Tasks.First().Id,
            myUserId, // 投稿者を自分自身にする
            DateTime.Now,
            "My own comment",
            null
        );
        newProject.Tasks.First().AddComment(newComment);

        var service = new ProjectDiffService();

        // Act
        var diff = service.CalculateDiff(oldProject, newProject, myUserId);

        // Assert
        Assert.IsFalse(
            diff.NewCommentTaskIds.Contains(task.Id),
            "自分自身の投稿したコメントが誤って検知されています。"
        );
    }
}
