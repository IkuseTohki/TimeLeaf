using System.Linq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// ユーザーがプロジェクトにアサインされているかを判定するユースケースの実装。
/// </summary>
public class CheckAssignmentUseCase : ICheckAssignmentUseCase
{
    private readonly IIdentityService _identityService;

    public CheckAssignmentUseCase(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public bool IsUserAssigned(Project project)
    {
        return project.AssignedUserIds.Any(id => id == _identityService.CurrentUserId);
    }
}
