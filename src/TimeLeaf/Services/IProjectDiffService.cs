using System;
using System.Collections.Generic;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

public record ProjectDiff(List<Guid> AssignedTaskIds, List<Guid> NewCommentTaskIds);

public interface IProjectDiffService
{
    ProjectDiff CalculateDiff(Project oldProject, Project newProject, Guid currentUserId);
}
