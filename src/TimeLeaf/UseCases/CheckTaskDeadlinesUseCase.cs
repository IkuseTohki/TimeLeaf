using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Services;
using TimeLeaf.Utilities;

namespace TimeLeaf.UseCases;

public class CheckTaskDeadlinesUseCase : ICheckTaskDeadlinesUseCase
{
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CheckTaskDeadlinesUseCase(INotificationService notificationService, IDateTimeProvider dateTimeProvider)
    {
        _notificationService = notificationService;
        _dateTimeProvider = dateTimeProvider;
    }

    public void Execute(IEnumerable<Project> projects)
    {
        var now = _dateTimeProvider.Now;
        var today = now.Date;

        foreach (var project in projects)
        {
            foreach (var task in project.Tasks)
            {
                if (task.Status == TaskStatus.Completed || !task.Deadline.HasValue)
                {
                    continue;
                }

                var deadlineDate = task.Deadline.Value.Date;

                if (deadlineDate < today)
                {
                    // 期限切れ
                    var notification = new Notification(
                        "タスク期限切れ",
                        $"タスク「{task.Name}」の期限を過ぎています。",
                        task.Id.ToString(),
                        task.Id
                    );
                    _notificationService.Notify(notification);
                }
                else if (deadlineDate <= today.AddDays(1))
                {
                    // 期限間近 (本日または明日)
                    var notification = new Notification(
                        "タスク期限間近",
                        $"タスク「{task.Name}」の期限が近づいています（期限：{deadlineDate:yyyy/MM/dd}）。",
                        task.Id.ToString()
                    );
                    _notificationService.Notify(notification);
                }
            }
        }
    }
}
