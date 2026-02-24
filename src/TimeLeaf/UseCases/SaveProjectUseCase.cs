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
    private readonly ICurrentUserService _userService;

    public SaveProjectUseCase(IProjectRepository repository, ICurrentUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    public async System.Threading.Tasks.Task ExecuteAsync(Project project)
    {
        var userId = _userService.GetCurrentUserId();
        await _repository.SaveAsync(project, userId);
    }
}
