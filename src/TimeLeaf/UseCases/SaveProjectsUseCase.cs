using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 全てのプロジェクトを保存するユースケース。
/// </summary>
public class SaveProjectsUseCase : ISaveProjectsUseCase
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _userService;

    public SaveProjectsUseCase(IProjectRepository repository, ICurrentUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    public async System.Threading.Tasks.Task ExecuteAsync(IEnumerable<Project> projects)
    {
        var userId = _userService.GetCurrentUserId();
        await _repository.SaveAllAsync(projects, userId);
    }
}
