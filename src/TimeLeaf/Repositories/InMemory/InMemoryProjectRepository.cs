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

    public System.Threading.Tasks.Task<IEnumerable<Project>> LoadAllAsync()
    {
        return System.Threading.Tasks.Task.FromResult(_projects.AsEnumerable());
    }

    public System.Threading.Tasks.Task SaveAllAsync(IEnumerable<Project> projects)
    {
        _projects.Clear();
        _projects.AddRange(projects);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
