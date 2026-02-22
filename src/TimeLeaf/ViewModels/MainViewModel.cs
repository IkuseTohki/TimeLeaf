using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LoadProjectsUseCase _loadUseCase;
    private readonly SaveProjectUseCase _saveSingleUseCase;
    private readonly IProjectRepository _repository;
    private readonly IAddProjectUseCase _addProjectUseCase;
    private readonly ILogger<MainViewModel> _logger;
    private readonly IServiceProvider _serviceProvider;

    private bool _isSyncing = false;

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    /// <summary>
    /// 全プロジェクトのリスト（メモリ内保持）。
    /// </summary>
    public ObservableCollection<ProjectViewModel> Projects { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="loadUseCase">プロジェクト読み込みユースケース。</param>
    /// <param name="saveSingleUseCase">単一プロジェクト保存ユースケース。</param>
    /// <param name="repository">イベント購読用リポジトリ（DIより注入）。</param>
    /// <param name="addProjectUseCase">プロジェクト追加ユースケース。</param>
    /// <param name="logger">ロガー。</param>
    /// <param name="serviceProvider">サービスプロバイダー。</param>
    public MainViewModel(LoadProjectsUseCase loadUseCase, SaveProjectUseCase saveSingleUseCase, IProjectRepository repository, IAddProjectUseCase addProjectUseCase, ILogger<MainViewModel> logger, IServiceProvider serviceProvider)
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _repository = repository;
        _addProjectUseCase = addProjectUseCase;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _currentViewModel = new OverviewViewModel(Projects, _addProjectUseCase, _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());

        _logger.LogInformation("MainViewModel Initializing");

        // 外部変更（同期）の監視
        _repository.ProjectChanged += OnProjectChanged;

        // 内部変更の監視と自動保存
        Projects.CollectionChanged += async (s, e) =>
        {
            if (_isSyncing) return;

            try
            {
                if (e.NewItems != null)
                {
                    foreach (ProjectViewModel itemViewModel in e.NewItems) // 型を ProjectViewModel に変更
                    {
                        _logger.LogDebug("New project detected in collection: {ProjectId}", itemViewModel.Id);
                        // ProjectViewModel内のTasksコレクションの変更を購読
                        itemViewModel.Tasks.CollectionChanged += async (ts, te) =>
                        {
                            if (!_isSyncing)
                            {
                                try
                                {
                                    // 個々のタスクの変更時にプロジェクト全体を保存する
                                    await _saveSingleUseCase.ExecuteAsync(itemViewModel.Model);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Failed to auto-save project {ProjectId} during task change", itemViewModel.Id);
                                }
                            }
                        };
                        // AddProjectUseCaseが保存を行うため、Projects.Addに起因する自動保存は不要になった
                        // (ただし、既存タスクの変更時に自動保存は必要なので、Tasks.CollectionChangedの購読は残す)
                        // await _saveSingleUseCase.ExecuteAsync(itemViewModel.Model); // AddProjectUseCaseが保存を行うため削除
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Projects.CollectionChanged handler");
            }
        };

        _ = InitializeAsync();

        _logger.LogInformation("MainViewModel Initializing Complete");
    }

    private void OnProjectChanged(Guid projectId)
    {
        _logger.LogInformation("Project changed event received for {ProjectId}", projectId);

        async System.Threading.Tasks.Task UpdateAction()
        {
            _isSyncing = true;
            try
            {
                var updatedProjectEntity = await _repository.LoadAsync(projectId);
                if (updatedProjectEntity == null) return;

                var existingViewModel = Projects.FirstOrDefault(pvm => pvm.Id == projectId); // ViewModelを検索
                if (existingViewModel != null)
                {
                    _logger.LogDebug("Updating existing project {ProjectId} ViewModel.", projectId);
                    existingViewModel.Name = updatedProjectEntity.Name; // ViewModel経由で更新
                    existingViewModel.Description = updatedProjectEntity.Description;
                    existingViewModel.Status = updatedProjectEntity.Status;
                    existingViewModel.HealthStatus = updatedProjectEntity.HealthStatus;

                    // Tasksコレクションの同期
                    existingViewModel.Model.Tasks.Clear();
                    existingViewModel.Tasks.Clear();
                    foreach (var t in updatedProjectEntity.Tasks)
                    {
                        existingViewModel.Model.Tasks.Add(t);
                        existingViewModel.Tasks.Add(new ProjectTaskViewModel(t));
                    }
                }
                else
                {
                    _logger.LogDebug("Adding new project {ProjectId} ViewModel from sync.", projectId);
                    var newProjectViewModel = new ProjectViewModel(updatedProjectEntity);
                    Projects.Add(newProjectViewModel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update project {ProjectId} on change event.", projectId);
            }
            finally
            {
                _isSyncing = false;
            }
        }

        // Dispatcher を介して実行（UIスレッドを担保）
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.InvokeAsync(UpdateAction);
        }
        else
        {
            _ = UpdateAction();
        }
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        _logger.LogInformation("Loading initial projects");
        _isSyncing = true;
        try
        {
            var projectEntities = await _loadUseCase.ExecuteAsync();
            foreach (var projectEntity in projectEntities)
            {
                var projectViewModel = new ProjectViewModel(projectEntity);
                projectViewModel.Tasks.CollectionChanged += async (s, e) => // ProjectViewModelのTasksを購読
                {
                    if (!_isSyncing)
                    {
                        try
                        {
                            // 個々のタスクの変更時にプロジェクト全体を保存する
                            await _saveSingleUseCase.ExecuteAsync(projectViewModel.Model);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to auto-save project {ProjectId} during task change", projectViewModel.Id);
                        }
                    }
                };
                Projects.Add(projectViewModel);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize projects");
        }
        finally
        {
            _isSyncing = false;
        }
        _logger.LogInformation("Loading initial projects Complete");
    }

    [RelayCommand]
    private void NavigateToProject(ProjectViewModel projectViewModel) // 引数の型を ViewModel に変更
    {
        if (projectViewModel == null) return;
        _logger.LogInformation("Navigating to project {ProjectId}", projectViewModel.Id);
        CurrentViewModel = new ProjectWorkspaceViewModel(projectViewModel.Model, _serviceProvider.GetRequiredService<ILogger<ProjectWorkspaceViewModel>>()); // Model を渡す
    }

    [RelayCommand]
    private void NavigateBack()
    {
        _logger.LogInformation("Navigating back to Overview");
        CurrentViewModel = new OverviewViewModel(Projects, _addProjectUseCase, _serviceProvider.GetRequiredService<ILogger<OverviewViewModel>>());
    }
}
