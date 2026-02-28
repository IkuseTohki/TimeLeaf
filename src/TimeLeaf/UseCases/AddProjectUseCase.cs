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
    private readonly ISaveProjectUseCase _saveUseCase;

    public AddProjectUseCase(ISaveProjectUseCase saveUseCase)
    {
        _saveUseCase = saveUseCase;
    }

    public async Task<Project> ExecuteAsync(string name, string description, ProjectStatus status, ProjectHealth health)
    {
        var project = new Project();
        project.UpdateName(name);
        project.UpdateDescription(description);
        project.UpdateStatus(status);
        project.UpdateHealth(health);

        await _saveUseCase.ExecuteAsync(project);
        return project;
    }
}
