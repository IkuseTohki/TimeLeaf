using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Utilities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクト内の期限が近いタスクを取得するユースケースの実装。
/// </summary>
public class GetProjectUpcomingDeadlinesUseCase : IGetProjectUpcomingDeadlinesUseCase
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetProjectUpcomingDeadlinesUseCase(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public IEnumerable<ProjectTask> Execute(Project project, int days)
    {
        var now = _dateTimeProvider.Now.Date;
        var limit = now.AddDays(days);

        return project
            .Tasks.Where(t =>
                t.Status != TaskStatus.Completed
                && t.Deadline.HasValue
                && t.Deadline.Value.Date >= now
                && t.Deadline.Value.Date <= limit
            )
            .OrderBy(t => t.Deadline);
    }
}
