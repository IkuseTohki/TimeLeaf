using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 全てのプロジェクトを読み込むユースケース。
/// IProjectService を介してキャッシュを初期化し、プロジェクト一覧を取得します。
/// </summary>
public class LoadProjectsUseCase : ILoadProjectsUseCase
{
    private readonly IProjectService _projectService;

    public LoadProjectsUseCase(IProjectService projectService)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
    }

    public async Task<IEnumerable<Project>> ExecuteAsync()
    {
        await _projectService.LoadAllAsync();
        return _projectService.AllProjects;
    }
}
