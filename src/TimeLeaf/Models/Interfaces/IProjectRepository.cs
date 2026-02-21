using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.Models.Interfaces;

/// <summary>
/// プロジェクトデータの永続化を担当するリポジトリのインターフェース。
/// </summary>
public interface IProjectRepository
{
    /// <summary>
    /// ストレージ上のデータが変更された際に通知されるイベント。
    /// 引数は変更があったプロジェクトのID。
    /// </summary>
    event Action<Guid>? ProjectChanged;

    /// <summary>
    /// 全てのプロジェクトを非同期で取得します。
    /// </summary>
    /// <returns>プロジェクトのリスト。</returns>
    System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync();

    /// <summary>
    /// 特定のプロジェクトを非同期で取得します。
    /// </summary>
    /// <param name="projectId">プロジェクトID。</param>
    /// <returns>プロジェクト。見つからない場合は null。</returns>
    System.Threading.Tasks.Task<Project?> LoadAsync(Guid projectId);

    /// <summary>
    /// プロジェクトのリストを非同期で保存します。
    /// </summary>
    /// <param name="projects">保存対象のプロジェクトリスト。</param>
    System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects);

    /// <summary>
    /// 単一のプロジェクトを非同期で保存します。
    /// </summary>
    /// <param name="project">保存対象のプロジェクト。</param>
    System.Threading.Tasks.Task SaveAsync(Project project);
}
