using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LoadProjectsUseCase _loadUseCase;
    private readonly SaveProjectUseCase _saveSingleUseCase;
    private readonly IProjectRepository _repository; // イベント購読のために保持

    [ObservableProperty]
    private ObservableObject _currentViewModel;

    /// <summary>
    /// 全プロジェクトのリスト（メモリ内保持）。
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="loadUseCase">プロジェクト読み込みユースケース。</param>
    /// <param name="saveSingleUseCase">単一プロジェクト保存ユースケース。</param>
    /// <param name="repository">イベント購読用リポジトリ（DIより注入）。</param>
    public MainViewModel(LoadProjectsUseCase loadUseCase, SaveProjectUseCase saveSingleUseCase, IProjectRepository repository)
    {
        _loadUseCase = loadUseCase;
        _saveSingleUseCase = saveSingleUseCase;
        _repository = repository;
        _currentViewModel = new OverviewViewModel(Projects);

        // 外部変更（同期）の監視
        _repository.ProjectChanged += OnProjectChanged;

        // 内部変更の監視と自動保存
        Projects.CollectionChanged += async (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (Project item in e.NewItems)
                {
                    item.Tasks.CollectionChanged += async (ts, te) => await _saveSingleUseCase.ExecuteAsync(item);
                    await _saveSingleUseCase.ExecuteAsync(item);
                }
            }
        };

        _ = InitializeAsync();
    }

    private void OnProjectChanged(Guid projectId)
    {
        // 更新処理の本体
        async System.Threading.Tasks.Task UpdateAction()
        {
            var updated = await _repository.LoadAsync(projectId);
            if (updated == null) return;

            var existing = Projects.FirstOrDefault(p => p.Id == projectId);
            if (existing != null)
            {
                existing.Name = updated.Name;
                existing.Tasks.Clear();
                foreach (var t in updated.Tasks) existing.Tasks.Add(t);
            }
            else
            {
                Projects.Add(updated);
            }
        }

        // Dispatcher を介して実行（UIスレッドを担保）
        if (Application.Current?.Dispatcher != null)
        {
            Application.Current.Dispatcher.InvokeAsync(UpdateAction);
        }
        else
        {
            // テスト環境など Dispatcher がない場合は直接実行
            _ = UpdateAction();
        }
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        var projects = await _loadUseCase.ExecuteAsync();
        foreach (var project in projects)
        {
            project.Tasks.CollectionChanged += async (s, e) => await _saveSingleUseCase.ExecuteAsync(project);
            Projects.Add(project);
        }
    }

    [RelayCommand]
    private void NavigateToProject(Project project)
    {
        if (project == null) return;
        CurrentViewModel = new ProjectWorkspaceViewModel(project);
    }

    [RelayCommand]
    private void NavigateBack()
    {
        CurrentViewModel = new OverviewViewModel(Projects);
    }
}
