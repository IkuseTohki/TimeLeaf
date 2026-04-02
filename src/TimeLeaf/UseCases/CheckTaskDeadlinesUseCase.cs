using System;
using System.Collections.Generic;
using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

public class CheckTaskDeadlinesUseCase : ICheckTaskDeadlinesUseCase
{
    private readonly INotificationService _notificationService;

    public CheckTaskDeadlinesUseCase(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public void Execute(IEnumerable<Project> projects)
    {
        var now = DateTime.Now;

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
                        task.Id.ToString(),
                        task.Id); // タスクIDをそのまま通知IDとしても使用する

                    _notificationService.Notify(notification);
                }
            }
        }
    }
}
