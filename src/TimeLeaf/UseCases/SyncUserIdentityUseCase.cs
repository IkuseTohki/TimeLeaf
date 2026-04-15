using System;
using System.Linq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases;

/// <summary>
/// 自分のアイデンティティをプロジェクトの共有ストレージに同期するユースケース。
/// </summary>
public class SyncUserIdentityUseCase : ISyncUserIdentityUseCase
{
    private readonly IIdentityService _identityService;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// ユースケースを初期化します。
    /// </summary>
    public SyncUserIdentityUseCase(IIdentityService identityService, IUserRepository userRepository)
    {
        _identityService = identityService;
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync()
    {
        // 1. 自分の ID を取得
        var myIdentity = await _identityService.GetCurrentIdentityAsync();

        // 2. root/users/{GUID}.json が既に存在するか確認
        var existingProfile = await _userRepository.GetUserAsync(myIdentity.Id);

        if (existingProfile == null)
        {
            // 3. 存在しない場合のみ、初期プロフィールを作成して保存
            // (名前やカラーは IdentityService が生成した初期値をそのまま使用)
            await _userRepository.SaveUserAsync(myIdentity);
        }
        else
        {
            // 4. 存在する場合、メモリ上のアイデンティティを既存プロフィールの内容で更新しておく
            // (これにより、UI で表示される自分の名前が root/users/ の内容と一致する)
            myIdentity.UpdateProfile(existingProfile.DisplayName, existingProfile.ThemeColor, existingProfile.IconPath);
        }
    }
}
