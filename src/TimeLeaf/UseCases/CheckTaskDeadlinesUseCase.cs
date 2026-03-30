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
                // Date プロパティ同士を比較することで、期限日の 23:59:59 までは「期限内」と判定されるようにする
                if (task.Status != TaskStatus.Completed && task.Deadline.HasValue && task.Deadline.Value.Date < now.Date)
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
