using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// ユーザーをプロジェクトに参加させる（アサインする）ユースケースの実装。
/// </summary>
public class JoinProjectUseCase : IJoinProjectUseCase
{
    private readonly IIdentityService _identityService;
    private readonly ISaveProjectUseCase _saveUseCase;

    public JoinProjectUseCase(IIdentityService identityService, ISaveProjectUseCase saveUseCase)
    {
        _identityService = identityService;
        _saveUseCase = saveUseCase;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(Project project)
    {
        if (project == null)
            throw new ArgumentNullException(nameof(project));

        var myId = _identityService.CurrentUserId;
        if (myId == Guid.Empty)
            throw new InvalidOperationException("Identity is not initialized.");

        // プロジェクトに自分をアサイン
        project.AssignUser(myId);

        // 変更を保存（LWW によりアサイン情報が同期される）
        await _saveUseCase.ExecuteAsync(project);
    }
}
