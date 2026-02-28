using System;
using System.Collections.ObjectModel;

namespace TimeLeaf.ViewModels;

/// <summary>
/// プロジェクトの変更を監視し、自動保存を調整するコーディネーターのインターフェース。
/// </summary>
public interface IProjectSaveCoordinator : IDisposable
{
    /// <summary>
    /// 自動保存が有効かどうかを取得または設定します。
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 指定されたコレクション内のプロジェクトの監視を開始します。
    /// </summary>
    /// <param name="projects">監視対象のプロジェクトコレクション。</param>
    void StartMonitoring(ObservableCollection<ProjectViewModel> projects);
}
