using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.UseCases;

/// <summary>
/// 全てのプロジェクトを保存するユースケース。
/// </summary>
public class SaveProjectsUseCase
{
    private readonly IProjectRepository _repository;

    public SaveProjectsUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task ExecuteAsync(IEnumerable<Project> projects)
    {
        await _repository.SaveAllAsync(projects);
    }
}
