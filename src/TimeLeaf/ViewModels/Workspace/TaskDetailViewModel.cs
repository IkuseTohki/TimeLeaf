using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクの完全な詳細表示、編集、および対話を担当するViewModel。
/// </summary>
public partial class TaskDetailViewModel : ObservableObject, LeafKit.UI.Services.IDialogViewModel
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ProjectTaskViewModel _taskViewModel;
    private readonly IAddCommentUseCase _addCommentUseCase;
    private readonly ILogger<TaskDetailViewModel> _logger;

    [ObservableProperty]
    private string _newCommentContent = string.Empty;

    public ProjectTaskViewModel Task => _taskViewModel;

    /// <inheritdoc />
    public event Action<bool>? RequestClose;

    /// <inheritdoc />
    public bool CanResize => true;

    public TaskDetailViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel,
        IAddCommentUseCase addCommentUseCase,
        ILogger<TaskDetailViewModel> logger
    )
    {
        _projectViewModel = projectViewModel;
        _taskViewModel = taskViewModel;
        _addCommentUseCase = addCommentUseCase;
        _logger = logger;
    }

    /// <summary>
    /// 詳細ウィンドウを閉じます。
    /// </summary>
    /// <param name="result">ダイアログの結果（true/false または文字列）。</param>
    [RelayCommand]
    private void Close(object? result)
    {
        bool dialogResult = result is bool b ? b : (result is string s && bool.TryParse(s, out var parsed) && parsed);
        RequestClose?.Invoke(dialogResult);
    }

    /// <summary>
    /// 新しいコメントを投稿します。
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddComment))]
    private async System.Threading.Tasks.Task AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentContent))
            return;

        try
        {
            await _addCommentUseCase.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Model, NewCommentContent);

            // 重要: ここで全件更新ではなく、必要な ViewModel のみを更新するのが理想だが、
            // 現状のドメインイベントの仕組みに合わせて最小限の同期を行う。
            // TODO: 今後、AddCommentUseCase が戻り値として新しい Comment を返し、
            // それを TaskViewModel.Comments に直接追加するように改善する。
            _projectViewModel.SyncFromModel();

            NewCommentContent = string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add comment.");
        }
    }

    private bool CanAddComment() => !string.IsNullOrWhiteSpace(NewCommentContent);

    partial void OnNewCommentContentChanged(string value)
    {
        AddCommentCommand.NotifyCanExecuteChanged();
    }
}
