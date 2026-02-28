using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいマイルストーンを追加し、永続化するユースケース。
/// </summary>
public class AddMilestoneUseCase : IAddMilestoneUseCase
{
    private readonly ISaveProjectUseCase _saveUseCase;

    public AddMilestoneUseCase(ISaveProjectUseCase saveUseCase)
    {
        _saveUseCase = saveUseCase;
    }

    public async Task ExecuteAsync(Project project, DateTime date, string label)
    {
        if (project == null) throw new ArgumentNullException(nameof(project));
        if (string.IsNullOrWhiteSpace(label)) throw new ArgumentException("Milestone label cannot be empty", nameof(label));

        project.AddMilestone(new Milestone { Date = date, Label = label });

        await _saveUseCase.ExecuteAsync(project);
    }
}
