using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.UseCases;

/// <summary>
/// 特定のIDを持つプロジェクトを1件取得するユースケース。
/// </summary>
public class FindProjectUseCase : IFindProjectUseCase
{
    private readonly IProjectRepository _repository;

    public FindProjectUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Project?> ExecuteAsync(Guid projectId)
    {
        return await _repository.LoadAsync(projectId);
    }
}
