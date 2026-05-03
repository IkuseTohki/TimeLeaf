using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトからタスクを削除するユースケース。
/// </summary>
public class DeleteTaskUseCase : IDeleteTaskUseCase
{
    private readonly IProjectService _projectService;

    public DeleteTaskUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Project project, Guid taskId)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        await _projectService.DeleteTaskAsync(project, taskId);
    }
}
