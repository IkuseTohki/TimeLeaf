using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// 特定のIDを持つプロジェクトを1件取得するユースケースのインターフェース。
/// </summary>
public interface IFindProjectUseCase
{
    Task<Project?> ExecuteAsync(Guid projectId);
}
