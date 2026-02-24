using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// 全てのプロジェクトを読み込むユースケースのインターフェース。
/// </summary>
public interface ILoadProjectsUseCase
{
    Task<IEnumerable<Project>> ExecuteAsync();
}
