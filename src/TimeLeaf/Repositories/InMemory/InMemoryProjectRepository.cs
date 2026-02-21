using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.Repositories.InMemory;

/// <summary>
/// メモリ上でプロジェクトデータを管理する一時的なリポジトリ。
/// </summary>
public class InMemoryProjectRepository : IProjectRepository
{
    private readonly List<Project> _projects = new();

    public event Action<Guid>? ProjectChanged;

    /// <summary>
    /// テスト用に、外部からのプロジェクト変更を通知します。
    /// </summary>
    /// <param name="projectId">変更があったプロジェクトID。</param>
    public void NotifyProjectChanged(Guid projectId)
    {
        ProjectChanged?.Invoke(projectId);
    }

    public System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync()
    {
        return System.Threading.Tasks.Task.FromResult(_projects.AsEnumerable());
    }

    public System.Threading.Tasks.Task<Project?> LoadAsync(Guid projectId)
    {
        return System.Threading.Tasks.Task.FromResult(_projects.FirstOrDefault(p => p.Id == projectId));
    }

    public System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        _projects.Clear();
        _projects.AddRange(projects);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public System.Threading.Tasks.Task SaveAsync(Project project)
    {
        var existing = _projects.FirstOrDefault(p => p.Id == project.Id);
        if (existing != null) _projects.Remove(existing);
        _projects.Add(project);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
