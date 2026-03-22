using System;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// ユーザーがプロジェクトにアサインされているかを判定するユースケース。
/// </summary>
public interface ICheckAssignmentUseCase
{
    bool IsUserAssigned(Project project);
}
