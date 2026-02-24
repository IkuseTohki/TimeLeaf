using System;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services;

/// <summary>
/// IProjectSyncService の実装。
/// リポジトリのイベントをリレーする。
/// </summary>
public class ProjectSyncService : IProjectSyncService
{
    private readonly IProjectRepository _repository;

    public event Action<Guid>? ProjectChanged;

    public ProjectSyncService(IProjectRepository repository)
    {
        _repository = repository;
        _repository.ProjectChanged += (id) => ProjectChanged?.Invoke(id);
    }

    public void StartMonitoring()
    {
        // 既存のリポジトリが既に監視を開始しているため、ここでは何もしない
    }

    public void StopMonitoring()
    {
        // 必要に応じて解除ロジックを実装
    }
}
