using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.ViewModels.Workspace;

/// <summary>
/// コメントを表示するための ViewModel。
/// 投稿者情報のリアクティブな解決に対応しています。
/// </summary>
public partial class CommentViewModel : ObservableObject, IDisposable
{
    private readonly Comment _comment;
    private readonly IUserService _userService;

    [ObservableProperty]
    private string _displayName = "Unknown User";

    [ObservableProperty]
    private string _authorInitial = "?";

    [ObservableProperty]
    private string _themeColor = "#9D9D9D";

    [ObservableProperty]
    private string? _iconPath;

    /// <summary>
    /// コメント本文。
    /// </summary>
    public string Content => _comment.Content;

    /// <summary>
    /// 投稿日時。
    /// </summary>
    public DateTime CreatedAt => _comment.CreatedAt;

    /// <summary>
    /// 投稿者のID。
    /// </summary>
    public Guid AuthorId => _comment.AuthorId;

    /// <summary>
    /// コンストラクタ。
    /// </summary>
    /// <param name="comment">コメントエンティティ。</param>
    /// <param name="userService">ユーザー情報を解決するためのサービス。</param>
    public CommentViewModel(Comment comment, IUserService userService)
    {
        _comment = comment ?? throw new ArgumentNullException(nameof(comment));
        _userService = userService ?? throw new ArgumentNullException(nameof(userService));

        // サービスの変更通知を購読
        _userService.UserChanged += OnUserChanged;

        // 初期解決を試みる
        _ = ResolveAuthorInfoAsync();
    }

    private async System.Threading.Tasks.Task ResolveAuthorInfoAsync()
    {
        var user = await _userService.GetUserAsync(_comment.AuthorId);
        if (user != null)
        {
            UpdateAuthorInfo(user.DisplayName, user.ThemeColor, user.IconPath);
        }
    }

    private void OnUserChanged(User user)
    {
        if (user != null && user.Id == _comment.AuthorId)
        {
            UpdateAuthorInfo(user.DisplayName, user.ThemeColor, user.IconPath);
        }
    }

    /// <summary>
    /// 投稿者の情報を更新します。
    /// </summary>
    /// <param name="name">表示名。</param>
    /// <param name="color">テーマカラー。</param>
    /// <param name="iconPath">アイコンパス。</param>
    private void UpdateAuthorInfo(string name, string color, string? iconPath)
    {
        DisplayName = name;
        ThemeColor = color;
        IconPath = iconPath;
        AuthorInitial = (name.Length >= 1 ? name.Substring(0, 1) : name).ToUpper();
    }

    public void Dispose()
    {
        _userService.UserChanged -= OnUserChanged;
    }
}
