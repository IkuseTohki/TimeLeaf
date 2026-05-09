using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいコンテナを追加し、永続化するユースケース。
/// </summary>
public class AddContainerUseCase : IAddContainerUseCase
{
    private readonly ISaveProjectUseCase _saveUseCase;
    private readonly ILogger<AddContainerUseCase> _logger;

    public AddContainerUseCase(ISaveProjectUseCase saveUseCase, ILogger<AddContainerUseCase> logger)
    {
        _saveUseCase = saveUseCase;
        _logger = logger;
    }

    public async Task ExecuteAsync(Project project, string name, string description, Guid? parentId)
    {
        _logger.LogInformation("Adding container: {Name}", name);
        var container = new ProjectContainer();
        container.UpdateName(name); // ここでバリデーションが行われる
        container.UpdateDescription(description);
        container.SetParentId(parentId);
        container.ProjectId = project.Id;

        project.AddContainer(container);

        _logger.LogInformation("Saving project after adding container.");
        await _saveUseCase.ExecuteAsync(project);
    }
}
