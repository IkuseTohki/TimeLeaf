using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

public class ProjectDiffService : IProjectDiffService
{
    public ProjectDiff CalculateDiff(Project oldProject, Project newProject, Guid currentUserId)
    {
        var assignedTaskIds = new List<Guid>();
        var newCommentTaskIds = new List<Guid>();

        foreach (var newTask in newProject.Tasks)
        {
            var oldTask = oldProject.Tasks.FirstOrDefault(t => t.Id == newTask.Id);

            // アサインチェック
            if (oldTask?.Assignee != currentUserId && newTask.Assignee == currentUserId)
            {
                assignedTaskIds.Add(newTask.Id);
            }

            // コメントチェック
            var oldCommentCount = oldTask?.Comments.Count ?? 0;
            if (newTask.Comments.Count > oldCommentCount)
            {
                newCommentTaskIds.Add(newTask.Id);
            }
        }

        return new ProjectDiff(assignedTaskIds, newCommentTaskIds);
    }
}
