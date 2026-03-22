using System;
using System.Collections.Generic;

namespace TimeLeaf.Repositories.FileSystem.Dtos;

/// <summary>
/// プロジェクトのアサインユーザー情報の永続化用 DTO。
/// </summary>
public class ProjectMembersDto
{
    /// <summary>
    /// アサインされているユーザーのIDリスト。
    /// </summary>
    public List<Guid> AssignedUserIds { get; set; } = new();
}
