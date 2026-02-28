using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクの詳細表示とコメント投稿を担当するViewModel。
/// </summary>
public partial class TaskDetailViewModel : ObservableObject
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ProjectTaskViewModel _taskViewModel;
    private readonly IAddCommentUseCase _addCommentUseCase;
    private readonly ILogger<TaskDetailViewModel> _logger;

    [ObservableProperty]
    private string _newCommentContent = string.Empty;

    public ProjectTaskViewModel Task => _taskViewModel;

    public TaskDetailViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel,
        IAddCommentUseCase addCommentUseCase,
        ILogger<TaskDetailViewModel> logger)
    {
        _projectViewModel = projectViewModel;
        _taskViewModel = taskViewModel;
        _addCommentUseCase = addCommentUseCase;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanAddComment))]
    private async System.Threading.Tasks.Task AddComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentContent)) return;

        try
        {
            await _addCommentUseCase.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Model, NewCommentContent);
            _projectViewModel.SyncFromModel(); // これにより TaskViewModel.Comments も更新される
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
