using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトからコンテナを削除するユースケースのインターフェース。
/// </summary>
public interface IDeleteContainerUseCase
{
    /// <summary>
    /// 指定されたコンテナを再帰的に削除します。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <param name="containerId">削除対象のコンテナID。</param>
    Task ExecuteAsync(Project project, Guid containerId);
}
