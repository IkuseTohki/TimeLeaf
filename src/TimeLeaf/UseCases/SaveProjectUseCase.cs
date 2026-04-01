using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 単一のプロジェクトを保存するユースケース。
/// </summary>
public class SaveProjectUseCase : ISaveProjectUseCase
{
    private readonly IProjectRepository _repository;
    private readonly IIdentityService _identityService;

    public SaveProjectUseCase(IProjectRepository repository, IIdentityService identityService)
    {
        _repository = repository;
        _identityService = identityService;
    }

    public async System.Threading.Tasks.Task ExecuteAsync(Project project)
    {
        var userId = _identityService.CurrentUserId.ToString();
        await _repository.SaveAsync(project, userId);
    }

    public async System.Threading.Tasks.Task ExecuteAsync(System.Collections.Generic.IEnumerable<Project> projects)
    {
        var userId = _identityService.CurrentUserId.ToString();
        await _repository.SaveAllAsync(projects, userId);
    }
}
