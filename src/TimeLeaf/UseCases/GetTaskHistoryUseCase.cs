using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクの変更履歴を取得するユースケースの実装。
/// </summary>
public class GetTaskHistoryUseCase : IGetTaskHistoryUseCase
{
    private readonly IProjectRepository _repository;

    public GetTaskHistoryUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ChangeRecord>> ExecuteAsync(Guid projectId, Guid taskId)
    {
        return await _repository.GetHistoryAsync(projectId, taskId);
    }
}
