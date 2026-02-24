using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.UseCases;

/// <summary>
/// 全てのプロジェクトを読み込むユースケース。
/// </summary>
public class LoadProjectsUseCase : ILoadProjectsUseCase
{
    private readonly IProjectRepository _repository;

    public LoadProjectsUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task<IEnumerable<Project>> ExecuteAsync()
    {
        return await _repository.LoadAllAsync();
    }
}
