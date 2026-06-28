using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class CommentResolutionTests
{
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();
    }

    /// <summary>
    /// テスト観点: コメントの投稿者情報（DisplayName）が、ユーザーリポジトリを介して正しく解決されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CommentAuthorName_ShouldBeResolvedFromService()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var displayName = "山田 太郎";
        var user = new User(userId, displayName, "#FF0000", "");

        _userServiceMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync(user);

        var task = new ProjectTask();
        var comment = new Comment
        {
            TaskId = task.Id,
            AuthorId = userId,
            Content = "テストコメント",
            CreatedAt = DateTime.Now,
        };
        task.AddComment(comment);

        // Act
        // ProjectTaskViewModel のコンストラクタ内で SyncComments() -> ResolveAuthorInfoAsync() が呼ばれる
        using var viewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);

        // 非同期の名前解決を待機
        await Task.Delay(100);

        // Assert
        var commentVm = viewModel.Comments.First();
        Assert.AreEqual(
            displayName,
            commentVm.DisplayName,
            "コメントの表示名がリポジトリから取得した名前に更新されていること"
        );
    }

    /// <summary>
    /// テスト観点: 名前解決に失敗した場合（ユーザーが見つからない場合）、デフォルトの表示名（Unknown User）が維持されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CommentAuthorName_ShouldBeUnknown_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _userServiceMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync((User)null!);

        var task = new ProjectTask();
        var comment = new Comment
        {
            TaskId = task.Id,
            AuthorId = userId,
            Content = "テストコメント",
        };
        task.AddComment(comment);

        // Act
        using var viewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);
        await Task.Delay(100);

        // Assert
        var commentVm = viewModel.Comments.First();
        Assert.AreEqual(
            "Unknown User",
            commentVm.DisplayName,
            "ユーザーが見つからない場合はデフォルトの表示名であること"
        );
    }

    /// <summary>
    /// テスト観点: ユーザー情報が後から更新された際（UserChangedイベント）、表示名が自動的に更新されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task CommentAuthorName_ShouldUpdate_WhenUserChangedEventFired()
    {
        // Arrange
        var userId = Guid.NewGuid();
        // 初期状態は Unknown
        _userServiceMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync((User)null!);

        var task = new ProjectTask();
        var comment = new Comment
        {
            TaskId = task.Id,
            AuthorId = userId,
            Content = "Hello",
        };
        task.AddComment(comment);

        using var viewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);
        await Task.Delay(100);
        Assert.AreEqual("Unknown User", viewModel.Comments.First().DisplayName);

        // Act: ユーザーが作成/更新されたイベントを発火
        var updatedUser = new User(userId, "後から来たユーザー", "#00FF00", "");
        _userServiceMock.Raise(s => s.UserChanged += null, updatedUser);

        // Assert: 即座に（またはイベント伝播後に）反映される
        Assert.AreEqual(
            "後から来たユーザー",
            viewModel.Comments.First().DisplayName,
            "イベント購読により表示名が更新されること"
        );
        Assert.AreEqual("#00FF00", viewModel.Comments.First().ThemeColor);
    }
}
