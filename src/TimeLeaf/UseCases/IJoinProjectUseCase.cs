using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// ユーザーをプロジェクトに参加させる（アサインする）ユースケースのインターフェース。
/// </summary>
public interface IJoinProjectUseCase
{
    /// <summary>
    /// 指定されたプロジェクトに自分自身をアサインします。
    /// </summary>
    /// <param name="project">対象のプロジェクト。</param>
    /// <returns>非同期タスク。</returns>
    Task ExecuteAsync(Project project);
}
