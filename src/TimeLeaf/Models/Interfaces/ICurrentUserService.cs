namespace TimeLeaf.Models.Interfaces;

/// <summary>
/// 現在のユーザー情報を取得するためのサービス。
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// 現在ログインしているユーザーのID。
    /// </summary>
    string GetCurrentUserId();
}
