using System.Collections.Generic;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトにアサインされているユーザーの一覧を取得するユースケース。
/// </summary>
public interface IGetProjectMembersUseCase
{
    Task<IEnumerable<User>> ExecuteAsync(Project project);
}
