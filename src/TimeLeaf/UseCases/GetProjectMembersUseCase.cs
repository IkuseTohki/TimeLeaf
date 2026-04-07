using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトにアサインされているユーザーの一覧を取得するユースケースの実装。
/// </summary>
public class GetProjectMembersUseCase : IGetProjectMembersUseCase
{
    private readonly IUserRepository _userRepository;

    public GetProjectMembersUseCase(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<User>> ExecuteAsync(Project project)
    {
        var members = new List<User>();
        foreach (var userId in project.AssignedUserIds)
        {
            var user = await _userRepository.GetUserAsync(userId);
            if (user != null)
            {
                members.Add(user);
            }
        }
        return members;
    }
}
