using System.Windows;
using System.Windows.Controls;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Views.Controls;

/// <summary>
/// タスクの進捗ステータスを選択するための共通コントロール。
/// </summary>
public partial class TaskStatusPicker : UserControl
{
    public static readonly DependencyProperty SelectedStatusProperty =
        DependencyProperty.Register(
            nameof(SelectedStatus),
            typeof(TaskStatus),
            typeof(TaskStatusPicker),
            new FrameworkPropertyMetadata(TaskStatus.NotStarted, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>
    /// 現在選択されているステータスを取得または設定します。
    /// </summary>
    public TaskStatus SelectedStatus
    {
        get => (TaskStatus)GetValue(SelectedStatusProperty);
        set => SetValue(SelectedStatusProperty, value);
    }

    public TaskStatusPicker()
    {
        InitializeComponent();
    }
}
