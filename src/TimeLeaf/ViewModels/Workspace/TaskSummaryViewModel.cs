using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクの概要表示と実績入力（クイック・サイドパネル）を担当するViewModel。
/// </summary>
public partial class TaskSummaryViewModel : ObservableObject, LeafKit.UI.Services.IDialogViewModel
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ProjectTaskViewModel _taskViewModel;

    public ProjectTaskViewModel Task => _taskViewModel;

    /// <summary>
    /// 現在のユーザーがプロジェクトにアサインされているかどうか。
    /// </summary>
    public bool IsAssignedToMe => _projectViewModel.IsAssignedToMe;

    /// <inheritdoc />
    public event Action<bool>? RequestClose;

    public TaskSummaryViewModel(ProjectViewModel projectViewModel, ProjectTaskViewModel taskViewModel)
    {
        _projectViewModel = projectViewModel ?? throw new ArgumentNullException(nameof(projectViewModel));
        _taskViewModel = taskViewModel ?? throw new ArgumentNullException(nameof(taskViewModel));
    }

    /// <summary>
    /// サイドパネルを閉じます。
    /// </summary>
    /// <param name="result">結果（通常は不使用だがインターフェース維持のため）。</param>
    [RelayCommand]
    private void Close(object? result)
    {
        bool dialogResult = result is bool b ? b : (result is string s && bool.TryParse(s, out var parsed) && parsed);
        RequestClose?.Invoke(dialogResult);
    }
}
