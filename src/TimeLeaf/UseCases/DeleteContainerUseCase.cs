using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトからコンテナを削除するユースケース。
/// </summary>
public class DeleteContainerUseCase : IDeleteContainerUseCase
{
    private readonly IProjectService _projectService;

    public DeleteContainerUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Project project, Guid containerId)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        await _projectService.DeleteContainerAsync(project, containerId);
    }
}
