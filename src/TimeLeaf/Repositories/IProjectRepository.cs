using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Repositories;

/// <summary>
/// プロジェクト情報を永続化・取得するためのリポジトリインターフェース。
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// 特定のプロジェクトが変更された際に発生するイベント。
    /// </summary>
    event Action<Guid> ProjectChanged;

    /// <summary>
    /// 全てのプロジェクトを非同期で読み込みます。
    /// </summary>
    Task<IEnumerable<Project>> LoadAllAsync();

    /// <summary>
    /// 指定されたIDのプロジェクトを非同期で読み込みます。
    /// </summary>
    Task<Project?> LoadAsync(Guid projectId);

    /// <summary>
    /// 指定されたプロジェクトを非同期で保存します。
    /// </summary>
    /// <param name="project">保存対象のプロジェクト。</param>
    /// <param name="userId">操作を実行しているユーザーのID。</param>
    Task SaveAsync(Project project, string userId);

    /// <summary>
    /// 複数のプロジェクトを一括で非同期保存します。
    /// </summary>
    /// <param name="projects">保存対象のプロジェクトリスト。</param>
    /// <param name="userId">操作を実行しているユーザーのID。</param>
    Task SaveAllAsync(IEnumerable<Project> projects, string userId);

    /// <summary>
    /// 指定されたプロジェクトを物理的に削除します。
    /// </summary>
    /// <param name="projectId">削除対象のプロジェクトID。</param>
    Task DeleteAsync(Guid projectId);
}
