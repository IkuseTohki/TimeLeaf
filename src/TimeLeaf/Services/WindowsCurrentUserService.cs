using System;
namespace TimeLeaf.Services;

/// <summary>
/// Windows のログインユーザー情報を取得するサービス。
/// </summary>
public class WindowsCurrentUserService : ICurrentUserService
{
    public string GetCurrentUserId()
    {
        // 開発・初期段階では Windows ユーザー名を ID として使用
        return Environment.UserName;
    }
}
