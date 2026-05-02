using System;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using Microsoft.Extensions.Logging;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクの完全な詳細表示、編集、および対話を担当するViewModel。
/// </summary>
public partial class TaskDetailViewModel : ObservableObject, IDialogViewModel
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ProjectTaskViewModel _taskViewModel;
    private readonly IAddCommentUseCase _addCommentUseCase;
    private readonly ISaveProjectUseCase _saveProjectUseCase;
    private readonly IDeleteTaskUseCase _deleteTaskUseCase;
    private readonly IDialogService _dialogService;
    private readonly ILogger<TaskDetailViewModel> _logger;

    [ObservableProperty]
    private string _newCommentContent = string.Empty;

    [ObservableProperty]
    private bool _isDirty;

    public ProjectTaskViewModel Task => _taskViewModel;
    public ProjectViewModel ProjectViewModel => _projectViewModel;

    /// <inheritdoc />
    public event Action<bool>? RequestClose;

    /// <inheritdoc />
    public bool CanResize => true;

    public TaskDetailViewModel(
        ProjectViewModel projectViewModel,
        ProjectTaskViewModel taskViewModel,
        IAddCommentUseCase addCommentUseCase,
        ISaveProjectUseCase saveProjectUseCase,
        IDeleteTaskUseCase deleteTaskUseCase,
        IDialogService dialogService,
        ILogger<TaskDetailViewModel> logger
    )
    {
        _projectViewModel = projectViewModel;
        _taskViewModel = taskViewModel;
        _addCommentUseCase = addCommentUseCase;
        _saveProjectUseCase = saveProjectUseCase;
        _deleteTaskUseCase = deleteTaskUseCase;
        _dialogService = dialogService;
        _logger = logger;

        _taskViewModel.PropertyChanged += OnTaskViewModelPropertyChanged;
    }

    private void OnTaskViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // UI 状態に関するプロパティ以外の変更を Dirty として扱う
        var importantProperties = new[]
        {
            nameof(ProjectTaskViewModel.Name),
            nameof(ProjectTaskViewModel.Description),
            nameof(ProjectTaskViewModel.Status),
            nameof(ProjectTaskViewModel.Priority),
            nameof(ProjectTaskViewModel.ScheduledStartDate),
            nameof(ProjectTaskViewModel.Deadline),
            nameof(ProjectTaskViewModel.ActualStartDate),
            nameof(ProjectTaskViewModel.ActualEndDate),
            nameof(ProjectTaskViewModel.EstimatedCost),
            nameof(ProjectTaskViewModel.ActualCost),
            nameof(ProjectTaskViewModel.Assignee),
        };

        if (importantProperties.Contains(e.PropertyName))
        {
            IsDirty = true;
        }
    }

    /// <summary>
    /// 変更を保存します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        try
        {
            await _saveProjectUseCase.ExecuteAsync(_projectViewModel.Model);
            IsDirty = false;
            _logger.LogInformation("Task changes saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save task changes.");
        }
    }

    /// <summary>
    /// 詳細ウィンドウを閉じます。
    /// </summary>
    /// <param name="result">ダイアログの結果。</param>
    [RelayCommand]
    private void Close(object? result)
    {
        bool dialogResult = result is bool b ? b : (result is string s && bool.TryParse(s, out var parsed) && parsed);
        RequestClose?.Invoke(dialogResult);
    }

    /// <summary>
    /// 戻るボタンの処理。未保存の変更がある場合は確認します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task Back()
    {
        if (IsDirty)
        {
            var result = _dialogService.ShowConfirmationDialog("変更を破棄して戻りますか？", "未保存の変更があります");
            if (!result)
                return;
        }

        RequestClose?.Invoke(false);
    }

    /// <summary>
    /// タスクを削除します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task Delete()
    {
        var result = _dialogService.ShowConfirmationDialog(
            $"タスク「{_taskViewModel.Name}」を削除してもよろしいですか？",
            "タスクの削除"
        );

        if (result)
        {
            try
            {
                await _deleteTaskUseCase.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Id);
                _projectViewModel.SyncFromModel();
                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete task.");
            }
        }
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
