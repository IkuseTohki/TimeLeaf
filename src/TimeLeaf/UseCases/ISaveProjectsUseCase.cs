using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// 複数のプロジェクトを保存するユースケースのインターフェース。
/// </summary>
public interface ISaveProjectsUseCase
{
    Task ExecuteAsync(IEnumerable<Project> projects);
}
