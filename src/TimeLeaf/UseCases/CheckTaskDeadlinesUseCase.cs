using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクの期限をチェックし、期限切れのタスクについて通知を発行するユースケース。
/// </summary>
public interface ICheckTaskDeadlinesUseCase
{
    void Execute(IEnumerable<Project> projects);
}

public class CheckTaskDeadlinesUseCase : ICheckTaskDeadlinesUseCase
{
    private readonly INotificationService _notificationService;

    public CheckTaskDeadlinesUseCase(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Execute(IEnumerable<Project> projects)
    {
        var now = DateTime.UtcNow;

        foreach (var project in projects)
        {
            foreach (var task in project.Tasks)
            {
                // 未完了かつ期限切れのタスクをチェック
                if (task.Status != TaskStatus.Completed && task.Deadline.HasValue && task.Deadline.Value < now)
                {
                    var notification = new Notification(
                        "タスク期限切れ",
                        $"タスク「{task.Name}」の期限を過ぎています。",
                        task.Id.ToString());

                    _notificationService.Notify(notification);
                }
            }
        }
    }
}
