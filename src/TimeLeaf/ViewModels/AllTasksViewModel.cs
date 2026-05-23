using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.ViewModels;

/// <summary>
/// 全プロジェクトのタスクを横断的に管理・表示するためのViewModel。
/// </summary>
public partial class AllTasksViewModel : ObservableObject
{
    private readonly ObservableCollection<ProjectViewModel> _projects;

    [ObservableProperty]
    private AllTasksViewMode _currentViewMode = AllTasksViewMode.List;

    /// <summary>
    /// 表示モード切替用のナビゲーション項目。
    /// </summary>
    public List<ViewModeItem> ViewModeItems { get; } =
        new()
        {
            new ViewModeItem("List", AllTasksViewMode.List),
            new ViewModeItem("Timeline", AllTasksViewMode.Timeline),
            new ViewModeItem("Grid", AllTasksViewMode.Grid),
        };

    /// <summary>
    /// 表示対象となるすべてのタスク。
    /// </summary>
    public ObservableCollection<ProjectTaskViewModel> AllTasks { get; } = new();

    /// <summary>
    /// グループ化やソートを適用したタスクビュー。
    /// </summary>
    public ICollectionView AllTasksView { get; }

    /// <summary>
    /// 全プロジェクトのリスト。
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects => _projects;

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private bool _showOnlyIncomplete = true;

    /// <summary>
    /// 特定のプロジェクト（およびオプションでタスク）への遷移が要求されたときに発生します。
    /// </summary>
    public event EventHandler<(ProjectViewModel Project, ProjectTaskViewModel? Task)>? RequestNavigation;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="projects">全プロジェクトのリスト。</param>
    public AllTasksViewModel(ObservableCollection<ProjectViewModel> projects)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        AllTasksView = CollectionViewSource.GetDefaultView(AllTasks);

        // 初期集約と全タスクへの監視設定
        foreach (var project in _projects)
        {
            project.Tasks.CollectionChanged += OnTasksCollectionChanged;
            foreach (var task in project.Tasks)
            {
                task.PropertyChanged += OnTaskPropertyChanged;
            }
        }

        RebuildTasks();

        // プロジェクトリストの変更を監視
        _projects.CollectionChanged += OnProjectsCollectionChanged;
    }

    private void ApplyGrouping()
    {
        AllTasksView.GroupDescriptions.Clear();
        if (CurrentViewMode == AllTasksViewMode.Timeline)
        {
            AllTasksView.GroupDescriptions.Add(
                new PropertyGroupDescription(nameof(ProjectTaskViewModel.DeadlineGroup))
            );
        }
    }

    partial void OnCurrentViewModeChanged(AllTasksViewMode value) => ApplyGrouping();

    [RelayCommand]
    private void SelectTask(ProjectTaskViewModel task)
    {
        if (task == null)
            return;
        var project = _projects.FirstOrDefault(p => p.Name == task.ProjectName);
        if (project != null)
        {
            RequestNavigation?.Invoke(this, (project, task));
        }
    }

    [RelayCommand]
    private void OpenTaskDetailWindow(ProjectTaskViewModel task)
    {
        SelectTask(task);
    }

    private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is ProjectViewModel p)
                {
                    p.Tasks.CollectionChanged -= OnTasksCollectionChanged;
                    foreach (var task in p.Tasks)
                    {
                        task.PropertyChanged -= OnTaskPropertyChanged;
                    }
                }
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is ProjectViewModel p)
                {
                    p.Tasks.CollectionChanged += OnTasksCollectionChanged;
                    foreach (var task in p.Tasks)
                    {
                        task.PropertyChanged += OnTaskPropertyChanged;
                    }
                }
            }
        }
        RebuildTasks();
    }

    private void OnTasksCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var item in e.OldItems)
            {
                if (item is ProjectTaskViewModel t)
                {
                    t.PropertyChanged -= OnTaskPropertyChanged;
                }
            }
        }
        if (e.NewItems != null)
        {
            foreach (var item in e.NewItems)
            {
                if (item is ProjectTaskViewModel t)
                {
                    t.PropertyChanged += OnTaskPropertyChanged;
                }
            }
        }
        RebuildTasks();
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // フィルタリングやソートに関係するプロパティが変更された場合のみ再構築
        var affectedProperties = new[]
        {
            nameof(ProjectTaskViewModel.Name),
            nameof(ProjectTaskViewModel.Status),
            nameof(ProjectTaskViewModel.Deadline),
            nameof(ProjectTaskViewModel.Priority),
        };
        if (affectedProperties.Contains(e.PropertyName))
        {
            RebuildTasks();
        }
    }

    partial void OnSearchKeywordChanged(string value) => RebuildTasks();

    partial void OnShowOnlyIncompleteChanged(bool value) => RebuildTasks();

    /// <summary>
    /// フィルタリングとソートを適用してタスクリストを再構築します。
    /// </summary>
    private void RebuildTasks()
    {
        var query = _projects.SelectMany(p => p.Tasks).AsEnumerable();

        // フィルタリング: キーワード
        if (!string.IsNullOrWhiteSpace(SearchKeyword))
        {
            query = query.Where(t =>
                t.Name.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)
                || t.ProjectName.Contains(SearchKeyword, StringComparison.OrdinalIgnoreCase)
            );
        }

        // フィルタリング: 未完了のみ
        if (ShowOnlyIncomplete)
        {
            query = query.Where(t => t.Status != TimeLeaf.Models.Enums.TaskStatus.Completed);
        }

        // ソート: 1.期限(昇順) 2.優先度(降順) 3.プロジェクト名(昇順)
        var sorted = query
            .OrderBy(t => t.Deadline ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.ProjectName)
            .ToList();

        // ObservableCollection を更新（全クリアして入れ直し）
        // TODO: 差分更新にすることでUIのチラつきを抑える
        AllTasks.Clear();
        foreach (var t in sorted)
        {
            AllTasks.Add(t);
        }
    }
}

/// <summary>
/// 表示モード切替項目を表すクラス。
/// </summary>
public class ViewModeItem
{
    public string Label { get; }
    public AllTasksViewMode Value { get; }

    public ViewModeItem(string label, AllTasksViewMode value)
    {
        Label = label;
        Value = value;
    }
}
