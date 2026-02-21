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
    /// 全てのプロジェクトを非同期で取得します。
    /// </summary>
    /// <returns>プロジェクトのリスト。</returns>
    System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync();

    /// <summary>
    /// プロジェクトのリストを非同期で保存します。
    /// </summary>
    /// <param name="projects">保存対象のプロジェクトリスト。</param>
    System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects);
}
