using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーション全体のプロジェクトおよびタスクの状態を管理するサービスのインターフェース。
/// キャッシュ保持、永続化の委譲、および変更通知の責務を持ちます。
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// 現在ロードされているすべてのプロジェクトを取得します。
    /// </summary>
    IEnumerable<Project> AllProjects { get; }

    /// <summary>
    /// 指定されたIDのプロジェクトを取得します。
    /// </summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>プロジェクトエンティティ。存在しない場合は null。</returns>
    Task<Project?> GetProjectAsync(Guid projectId);

    /// <summary>
    /// すべてのプロジェクトをストレージからロードし、キャッシュを初期化します。
    /// </summary>
    Task LoadAllAsync();

    /// <summary>
    /// プロジェクトを保存し、キャッシュを更新します。
    /// </summary>
    /// <param name="project">保存するプロジェクト。</param>
    Task SaveProjectAsync(Project project);

    /// <summary>
    /// 複数のプロジェクトを一括で非同期保存します。
    /// </summary>
    /// <param name="projects">保存対象のプロジェクトリスト。</param>
    Task SaveAllAsync(IEnumerable<Project> projects);

    /// <summary>
    /// タスクを削除します。
    /// </summary>
    Task DeleteTaskAsync(Project project, Guid taskId);

    /// <summary>
    /// プロジェクトが追加された際に発生します。
    /// </summary>
    event Action<Project>? ProjectAdded;

    /// <summary>
    /// プロジェクトの状態（名前、説明、タスク、コメント等）が更新された際に発生します。
    /// </summary>
    event Action<Project>? ProjectUpdated;

    /// <summary>
    /// プロジェクトが削除された際に発生します。
    /// </summary>
    event Action<Guid>? ProjectRemoved;
}
