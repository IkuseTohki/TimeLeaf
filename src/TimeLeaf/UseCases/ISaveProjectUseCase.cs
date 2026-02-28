using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// 単一のプロジェクトを保存するユースケースのインターフェース。
/// </summary>
public interface ISaveProjectUseCase
{
    Task ExecuteAsync(Project project);
    Task ExecuteAsync(IEnumerable<Project> projects);
}
