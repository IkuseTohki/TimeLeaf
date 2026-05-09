using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいコンテナを追加し、永続化するユースケースのインターフェース。
/// </summary>
public interface IAddContainerUseCase
{
    Task ExecuteAsync(Project project, string name, string description, Guid? parentId);
}
