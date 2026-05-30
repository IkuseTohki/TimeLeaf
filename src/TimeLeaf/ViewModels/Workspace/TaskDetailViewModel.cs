using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// タスクの完全な詳細表示、編集、および対話を担当するViewModel。
/// </summary>
public partial class TaskDetailViewModel : ObservableObject, IDialogViewModel, IDisposable
{
    private readonly ProjectViewModel _projectViewModel;
    private readonly ProjectTaskViewModel _taskViewModel;
    private readonly IAddCommentUseCase _addCommentUseCase;
    private readonly ISaveProjectUseCase _saveProjectUseCase;
    private readonly IDeleteTaskUseCase _deleteTaskUseCase;
    private readonly IDialogService _dialogService;
    private readonly IUserService _userService;
    private readonly IProjectService _projectService;
    private readonly IGetProjectMembersUseCase _getProjectMembersUseCase;
    private readonly ILogger<TaskDetailViewModel> _logger;

    private readonly ProjectTaskViewModel _workingTaskViewModel;
    private readonly ProjectTask _workingTask;

    [ObservableProperty]
    private string _newCommentContent = string.Empty;

    [ObservableProperty]
    private ObservableCollection<User> _availableTeammates = new();

    [ObservableProperty]
    private User? _selectedAssignee;

    [ObservableProperty]
    private bool _isDirty;

    [ObservableProperty]
    private bool _hasExternalChange;

    public ProjectTaskViewModel Task => _workingTaskViewModel;
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
        IUserService userService,
        IProjectService projectService,
        ILogger<TaskDetailViewModel> logger,
        IGetProjectMembersUseCase getProjectMembersUseCase
    )
    {
        _projectViewModel = projectViewModel;
        _taskViewModel = taskViewModel;
        _addCommentUseCase = addCommentUseCase;
        _saveProjectUseCase = saveProjectUseCase;
        _deleteTaskUseCase = deleteTaskUseCase;
        _dialogService = dialogService;
        _userService = userService;
        _projectService = projectService;
        _logger = logger;
        _getProjectMembersUseCase = getProjectMembersUseCase;

        // 作業用コピーの作成
        _workingTask = _taskViewModel.Model.Clone();
        _workingTaskViewModel = new ProjectTaskViewModel(_workingTask, _userService);
        _workingTaskViewModel.ProjectName = _taskViewModel.ProjectName;

        _workingTaskViewModel.PropertyChanged += OnWorkingTaskViewModelPropertyChanged;

        // 外部変更の監視
        _projectService.ProjectUpdated += OnProjectServiceProjectUpdated;
    }

    public async Task LoadMembersAsync()
    {
        var members = await _getProjectMembersUseCase.ExecuteAsync(_projectViewModel.Model);
        AvailableTeammates = new ObservableCollection<User>(members);

        // 現在の担当者をセット
        SelectedAssignee = AvailableTeammates.FirstOrDefault(u => u.Id == Task.Assignee);
    }

    partial void OnSelectedAssigneeChanged(User? value)
    {
        if (Task.Assignee != value?.Id)
        {
            Task.Assignee = value?.Id;
        }
    }

    private void OnProjectServiceProjectUpdated(Project project)
    {
        if (project.Id == _projectViewModel.Model.Id)
        {
            // 他ユーザーによる変更（または自身の別操作による変更）を検知
            HasExternalChange = true;
        }
    }

    private void OnWorkingTaskViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

        if (e.PropertyName == nameof(ProjectTaskViewModel.Assignee))
        {
            SelectedAssignee = AvailableTeammates.FirstOrDefault(u => u.Id == Task.Assignee);
        }
    }

    /// <summary>
    /// 最新のデータを読み込み直します。
    /// </summary>
    [RelayCommand]
    private void ReloadLatest()
    {
        if (IsDirty)
        {
            var result = _dialogService.ShowConfirmationDialog(
                "現在の編集内容を破棄して、最新のデータを読み込みますか？",
                "データの再読み込み"
            );
            if (!result)
                return;
        }

        // マスターモデルは ProjectService 経由で既に最新になっているはずなので、そこからクローンし直す
        var latestTask = _projectViewModel.Model.Tasks.FirstOrDefault(t => t.Id == _taskViewModel.Id);
        if (latestTask != null)
        {
            _workingTask.MergeFrom(latestTask);
            _workingTaskViewModel.UpdateFromModel(_workingTask);
            IsDirty = false;
            HasExternalChange = false;
        }
    }

    /// <summary>
    /// 変更を保存します。
    /// </summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task Save()
    {
        if (HasExternalChange)
        {
            var result = _dialogService.ShowConfirmationDialog(
                "他ユーザーによる変更があります。上書きして保存しますか？",
                "保存の確認"
            );
            if (!result)
                return;
        }

        try
        {
            // 作業内容をマスターにマージ
            _taskViewModel.Model.MergeFrom(_workingTask);
            _taskViewModel.UpdateFromModel(_taskViewModel.Model);

            await _saveProjectUseCase.ExecuteAsync(_projectViewModel.Model);
            IsDirty = false;
            HasExternalChange = false;
            _logger.LogInformation("Task changes saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save task changes.");
        }
    }

    /// <summary>
    /// 詳細ウィンドウを閉じます。未保存の変更がある場合は確認します。
    /// </summary>
    /// <param name="result">ダイアログの結果。</param>
    [RelayCommand]
    private void Close(object? result)
    {
        if (IsDirty)
        {
            var confirm = _dialogService.ShowConfirmationDialog("変更を破棄して閉じますか？", "未保存の変更があります");
            if (!confirm)
                return;
        }

        bool dialogResult = result is bool b ? b : (result is string s && bool.TryParse(s, out var parsed) && parsed);
        RequestClose?.Invoke(dialogResult);
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
            // コメントはマスターに対して直接追加し保存する（作業用コピーではなく）
            await _addCommentUseCase.ExecuteAsync(_projectViewModel.Model, _taskViewModel.Model, NewCommentContent);

            // 作業用コピー側にも反映させて表示を更新
            _workingTask.AddComment(_taskViewModel.Model.Comments.Last());
            _workingTaskViewModel.UpdateFromModel(_workingTask);

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

    public void Dispose()
    {
        _projectService.ProjectUpdated -= OnProjectServiceProjectUpdated;
        _workingTaskViewModel.PropertyChanged -= OnWorkingTaskViewModelPropertyChanged;
        _workingTaskViewModel.Dispose();
    }
}
