using System.Windows;
using System.Windows.Controls;

namespace TimeLeaf.Views.Controls;

/// <summary>
/// タスクの見積工数と実績工数を編集・表示するための共通コントロール。
/// </summary>
public partial class TaskCostEditor : UserControl
{
    public static readonly DependencyProperty EstimatedCostProperty =
        DependencyProperty.Register(nameof(EstimatedCost), typeof(double), typeof(TaskCostEditor), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty ActualCostProperty =
        DependencyProperty.Register(nameof(ActualCost), typeof(double), typeof(TaskCostEditor), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(TaskCostEditor), new PropertyMetadata(false));

    public double EstimatedCost
    {
        get => (double)GetValue(EstimatedCostProperty);
        set => SetValue(EstimatedCostProperty, value);
    }

    public double ActualCost
    {
        get => (double)GetValue(ActualCostProperty);
        set => SetValue(ActualCostProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public TaskCostEditor()
    {
        InitializeComponent();
    }
}
