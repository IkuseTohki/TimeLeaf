using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクにコメントを追加し、プロジェクトを永続化するユースケースのインターフェース。
/// </summary>
public interface IAddCommentUseCase
{
    Task ExecuteAsync(Project project, ProjectTask task, string content);
}
