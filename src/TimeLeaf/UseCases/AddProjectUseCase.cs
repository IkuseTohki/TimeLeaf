using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 新規プロジェクトを追加するユースケース。
/// </summary>
public class AddProjectUseCase : IAddProjectUseCase
{
    private readonly IProjectService _projectService;

    public AddProjectUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    public async Task<Project> ExecuteAsync(string name, string description, ProjectStatus status, ProjectHealth health)
    {
        var project = new Project();
        project.UpdateBasicInfo(name, status, health);
        project.UpdateDescription(description);

        await _projectService.SaveProjectAsync(project);
        return project;
    }
}
