using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly LoadProjectsUseCase _loadUseCase;
    private readonly SaveProjectsUseCase _saveUseCase;

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
    /// <param name="saveUseCase">プロジェクト保存ユースケース。</param>
    public MainViewModel(LoadProjectsUseCase loadUseCase, SaveProjectsUseCase saveUseCase)
    {
        _loadUseCase = loadUseCase;
        _saveUseCase = saveUseCase;
        _currentViewModel = new OverviewViewModel(Projects);

        // 変更を監視して自動保存
        Projects.CollectionChanged += async (s, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (Project item in e.NewItems)
                {
                    item.Tasks.CollectionChanged += async (ts, te) => await SaveAsync();
                }
            }
            await SaveAsync();
        };

        // 非同期ロードを開始
        _ = InitializeAsync();
    }

    private async System.Threading.Tasks.Task SaveAsync()
    {
        await _saveUseCase.ExecuteAsync(Projects);
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        var projects = await _loadUseCase.ExecuteAsync();
        foreach (var project in projects)
        {
            project.Tasks.CollectionChanged += async (s, e) => await SaveAsync();
            Projects.Add(project);
        }
    }

    /// <summary>
    /// 指定されたプロジェクトのワークスペース画面に遷移するコマンド。
    /// </summary>
    /// <param name="project">対象プロジェクト。</param>
    [RelayCommand]
    private void NavigateToProject(Project project)
    {
        if (project == null) return;
        CurrentViewModel = new ProjectWorkspaceViewModel(project);
    }

    /// <summary>
    /// トップ画面に戻るコマンド。
    /// </summary>
    [RelayCommand]
    private void NavigateBack()
    {
        CurrentViewModel = new OverviewViewModel(Projects);
    }
}
