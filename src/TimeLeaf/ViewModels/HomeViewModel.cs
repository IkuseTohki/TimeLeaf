using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TimeLeaf.ViewModels;

/// <summary>
/// ホーム（グローバルダッシュボード）の ViewModel。
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    public ObservableCollection<ProjectViewModel> Projects { get; }
    public UserMenuViewModel UserMenu { get; }

    [ObservableProperty]
    private int _notStartedCount;

    [ObservableProperty]
    private int _inProgressCount;

    [ObservableProperty]
    private int _completedThisWeekCount;

    [ObservableProperty]
    private bool _isUserMenuOpen;

    [RelayCommand]
    private void ToggleUserMenu() => IsUserMenuOpen = !IsUserMenuOpen;

    /// <summary>
    /// 全プロジェクトの期限が近いタスク。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> UpcomingDeadlines { get; } = new();

    /// <summary>
    /// 全プロジェクトの直近のマイルストーン。
    /// </summary>
    public ObservableCollection<MilestoneWithProject> UpcomingMilestones { get; } = new();

    public HomeViewModel(ObservableCollection<ProjectViewModel> projects, UserMenuViewModel userMenu)
    {
        Projects = projects;
        UserMenu = userMenu;

        // 初回計算
        UpdateStats();

        // プロジェクトリストの変更を監視（簡略化のため全件再計算）
        Projects.CollectionChanged += (s, e) => UpdateStats();
    }

    private void UpdateStats()
    {
        var allTasks = Projects.SelectMany(p => p.Tasks).ToList();

        NotStartedCount = allTasks.Count(t => t.Status == TimeLeaf.Models.Enums.TaskStatus.NotStarted);
        InProgressCount = allTasks.Count(t => t.Status == TimeLeaf.Models.Enums.TaskStatus.InProgress);

        // 今週完了したタスクの計算（月曜日開始と仮定）
        var now = DateTime.Now;
        var startOfWeek = now.AddDays(-(int)now.DayOfWeek + (int)DayOfWeek.Monday).Date;
        CompletedThisWeekCount = allTasks.Count(t =>
            t.Status == TimeLeaf.Models.Enums.TaskStatus.Completed &&
            t.ActualEndDate >= startOfWeek);

        // 期限が近いタスク（未完了かつ期限あり）
        UpcomingDeadlines.Clear();
        var deadlines = allTasks
            .Where(t => t.Status != TimeLeaf.Models.Enums.TaskStatus.Completed && t.Deadline.HasValue)
            .OrderBy(t => t.Deadline)
            .Take(5);
        foreach (var t in deadlines) UpcomingDeadlines.Add(t);

        // マイルストーンの集約
        UpcomingMilestones.Clear();
        var milestones = Projects
            .SelectMany(p => p.Model.Milestones.Select(m => new MilestoneWithProject(p.Name, m.Label, m.Date)))
            .Where(m => m.Date >= now.Date)
            .OrderBy(m => m.Date)
            .Take(5);
        foreach (var m in milestones) UpcomingMilestones.Add(m);
    }
}

/// <summary>
/// プロジェクト名付きのマイルストーン情報。
/// </summary>
public record MilestoneWithProject(string ProjectName, string Label, DateTime Date);
