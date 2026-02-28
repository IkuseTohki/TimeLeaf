using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// プロジェクトに新しいマイルストーンを追加し、永続化するユースケースのインターフェース。
/// </summary>
public interface IAddMilestoneUseCase
{
    Task ExecuteAsync(Project project, DateTime date, string label);
}
