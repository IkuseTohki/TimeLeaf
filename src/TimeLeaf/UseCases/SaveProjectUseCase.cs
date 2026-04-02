using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトを保存するユースケース。
/// IProjectService を介して永続化とキャッシュ更新を行います。
/// </summary>
public class SaveProjectUseCase : ISaveProjectUseCase
{
    private readonly IProjectService _projectService;

    public SaveProjectUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    public async Task ExecuteAsync(Project project)
    {
        await _projectService.SaveProjectAsync(project);
    }

    public async Task ExecuteAsync(IEnumerable<Project> projects)
    {
        await _projectService.SaveAllAsync(projects);
    }
}
