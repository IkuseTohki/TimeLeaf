namespace TimeLeaf.Services;

/// <summary>
/// 現在のユーザー情報を取得するサービスのインターフェース。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 現在のユーザーIDを取得します。
    /// </summary>
    /// <returns>ユーザーID（文字列）。</returns>
    string GetCurrentUserId();
}
