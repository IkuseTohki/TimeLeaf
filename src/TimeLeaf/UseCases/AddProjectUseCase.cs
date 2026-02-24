using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using System.Threading.Tasks;

namespace TimeLeaf.UseCases;

/// <summary>
/// 新しいプロジェクトを作成し、永続化するユースケース。
/// </summary>
public class AddProjectUseCase : IAddProjectUseCase
{
    private readonly IProjectRepository _repository;
    private readonly ICurrentUserService _userService;

    public AddProjectUseCase(IProjectRepository repository, ICurrentUserService userService)
    {
        _repository = repository;
        _userService = userService;
    }

    public async Task<Project> ExecuteAsync(string name, string description, ProjectStatus status, ProjectHealth health)
    {
        var project = new Project();
        project.UpdateName(name);
        project.UpdateDescription(description);
        project.UpdateStatus(status);
        project.UpdateHealth(health);

        var userId = _userService.GetCurrentUserId();
        await _repository.SaveAsync(project, userId);
        return project;
    }
}
