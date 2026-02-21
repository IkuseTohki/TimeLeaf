using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のメインViewModel。ナビゲーション（画面遷移）を管理する。
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableObject _currentViewModel;

    /// <summary>
    /// 全プロジェクトのリスト（メモリ内保持）。
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>
    /// コンストラクタ。初期画面をプロジェクトオーバービューに設定する。
    /// </summary>
    public MainViewModel()
    {
        _currentViewModel = new OverviewViewModel(Projects);
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
