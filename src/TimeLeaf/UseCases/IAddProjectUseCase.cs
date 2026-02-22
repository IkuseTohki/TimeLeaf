using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.UseCases;

public interface IAddProjectUseCase
{
    Task<Project> ExecuteAsync(string name, string description, ProjectStatus status, ProjectHealth health);
}
