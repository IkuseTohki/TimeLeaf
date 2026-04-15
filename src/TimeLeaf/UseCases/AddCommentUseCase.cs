using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// タスクにコメントを追加し、プロジェクトを永続化するユースケース。
/// </summary>
public class AddCommentUseCase : IAddCommentUseCase
{
    private readonly ISaveProjectUseCase _saveUseCase;
    private readonly IIdentityService _identityService;

    public AddCommentUseCase(ISaveProjectUseCase saveUseCase, IIdentityService identityService)
    {
        _saveUseCase = saveUseCase;
        _identityService = identityService;
    }

    public async Task ExecuteAsync(Project project, ProjectTask task, string content)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty", nameof(content));

        var comment = new Comment
        {
            TaskId = task.Id,
            AuthorId = _identityService.CurrentUserId,
            Content = content,
            CreatedAt = DateTime.Now,
        };

        task.AddComment(comment);

        await _saveUseCase.ExecuteAsync(project);
    }
}
