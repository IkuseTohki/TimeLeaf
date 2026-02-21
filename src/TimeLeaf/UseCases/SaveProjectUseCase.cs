using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;

namespace TimeLeaf.UseCases;

/// <summary>
/// 単一のプロジェクトを保存するユースケース。
/// </summary>
public class SaveProjectUseCase
{
    private readonly IProjectRepository _repository;

    public SaveProjectUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async System.Threading.Tasks.Task ExecuteAsync(Project project)
    {
        await _repository.SaveAsync(project);
    }
}
