using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Models.Interfaces;
using System.Threading.Tasks;

namespace TimeLeaf.UseCases;

/// <summary>
/// 新しいプロジェクトを作成し、永続化するユースケース。
/// </summary>
public class AddProjectUseCase : IAddProjectUseCase
{
    private readonly IProjectRepository _repository;

    public AddProjectUseCase(IProjectRepository repository)
    {
        _repository = repository;
    }

    public async Task<Project> ExecuteAsync(string name, string description, ProjectStatus status, ProjectHealth health)
    {
        var project = new Project
        {
            Name = name,
            Description = description,
            Status = status,
            HealthStatus = health
        };

        await _repository.SaveAsync(project);
        return project;
    }
}
