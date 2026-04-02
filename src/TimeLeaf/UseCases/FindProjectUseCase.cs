using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 特定のプロジェクトを検索・取得するユースケース。
/// IProjectService を介してキャッシュ（またはリポジトリ）から取得します。
/// </summary>
public class FindProjectUseCase : IFindProjectUseCase
{
    private readonly IProjectService _projectService;

    public FindProjectUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    public async Task<Project?> ExecuteAsync(Guid projectId)
    {
        return await _projectService.GetProjectAsync(projectId);
    }
}
