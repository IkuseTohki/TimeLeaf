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
    private readonly ProjectTaskViewModel _taskViewModel;

    public ProjectTaskViewModel Task => _taskViewModel;

    /// <inheritdoc />
    public event Action<bool>? RequestClose;

    public TaskSummaryViewModel(ProjectTaskViewModel taskViewModel)
    {
        _taskViewModel = taskViewModel;
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
