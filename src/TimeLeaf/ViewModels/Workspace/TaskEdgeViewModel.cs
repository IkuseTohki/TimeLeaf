using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスク間の依存関係（エッジ）を表示するための ViewModel。
/// </summary>
public partial class TaskEdgeViewModel : ObservableObject
{
    private readonly ProjectTaskViewModel _from;
    private readonly ProjectTaskViewModel _to;

    /// <summary>先行タスクのID。</summary>
    public Guid FromId => _from.Id;

    /// <summary>後続タスクのID。</summary>
    public Guid ToId => _to.Id;

    /// <summary>始点のX座標（先行タスクの中心付近）。</summary>
    public double X1 => _from.X + 70;

    /// <summary>始点のY座標（先行タスクの中心付近）。</summary>
    public double Y1 => _from.Y + 20;

    /// <summary>終点のX座標（後続タスクの中心付近）。</summary>
    public double X2 => _to.X + 70;

    /// <summary>終点のY座標（後続タスクの中心付近）。</summary>
    public double Y2 => _to.Y + 20;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="from">先行タスクの ViewModel。</param>
    /// <param name="to">後続タスクの ViewModel。</param>
    public TaskEdgeViewModel(ProjectTaskViewModel from, ProjectTaskViewModel to)
    {
        _from = from ?? throw new ArgumentNullException(nameof(from));
        _to = to ?? throw new ArgumentNullException(nameof(to));

        // 座標変更を購読して自身の座標プロパティを通知
        _from.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectTaskViewModel.X) || e.PropertyName == nameof(ProjectTaskViewModel.Y))
            {
                OnPropertyChanged(nameof(X1));
                OnPropertyChanged(nameof(Y1));
            }
        };
        _to.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ProjectTaskViewModel.X) || e.PropertyName == nameof(ProjectTaskViewModel.Y))
            {
                OnPropertyChanged(nameof(X2));
                OnPropertyChanged(nameof(Y2));
            }
        };
    }
}
