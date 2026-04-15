using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class FileSystemUserRepositoryTests
{
    private string _testRoot = null!;
    private string _usersDir = null!;

    [TestInitialize]
    public void Initialize()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "TimeLeafTests", Guid.NewGuid().ToString());
        _usersDir = Path.Combine(_testRoot, "users");
        Directory.CreateDirectory(_usersDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, true);
        }
    }

    /// <summary>
    /// テスト観点: ユーザーを保存し、正しく読み込めることを確認する。
    /// </summary>
    [TestMethod]
    public async Task SaveAndGet_ShouldSucceed()
    {
        // Arrange
        var repository = new FileSystemUserRepository(_usersDir);
        var user = new User(Guid.NewGuid(), "田中 太郎", "#FF0000", "user1.png");

        // Act
        await repository.SaveUserAsync(user);
        var loadedUser = await repository.GetUserAsync(user.Id);

        // Assert
        Assert.IsNotNull(loadedUser);
        Assert.AreEqual(user.Id, loadedUser.Id);
        Assert.AreEqual(user.DisplayName, loadedUser.DisplayName);
        Assert.AreEqual(user.ThemeColor, loadedUser.ThemeColor);
        Assert.AreEqual(user.IconPath, loadedUser.IconPath);
    }

    /// <summary>
    /// テスト観点: ファイルシステム上のユーザーファイルが更新された際に、リポジトリが UserChanged イベントを発行することを確認する。
    /// </summary>
    [TestMethod]
    public async Task FileUpdate_ShouldRaiseUserChangedEvent()
    {
        // Arrange
        var repository = new FileSystemUserRepository(_usersDir);
        var userId = Guid.NewGuid();
        var user = new User(userId, "Original Name", "#FF0000", "user1.png");
        await repository.SaveUserAsync(user);

        var eventRaised = false;
        Guid? changedUserId = null;
        repository.UserChanged += (s, e) =>
        {
            eventRaised = true;
            changedUserId = e;
        };

        // Act - ファイルを直接書き換えて外部からの更新をシミュレート
        var filePath = Path.Combine(_usersDir, $"{userId}.json");
        var updatedJson = File.ReadAllText(filePath).Replace("Original Name", "Updated Name");

        // Windows のファイルシステムの遅延やロックを考慮しつつ書き込み
        File.WriteAllText(filePath, updatedJson);

        // Assert - 非同期イベントを待機
        for (int i = 0; i < 10 && !eventRaised; i++)
        {
            await Task.Delay(100);
        }

        Assert.IsTrue(eventRaised, "ユーザー情報の変更イベントが発行されること");
        Assert.AreEqual(userId, changedUserId, "変更されたユーザーのIDが正しいこと");
    }

    /// <summary>
    /// テスト観点: 複数のユーザーを保存し、一覧取得できることを確認する。
    /// </summary>
    [TestMethod]
    public async Task GetAllUsers_ShouldReturnAllSavedUsers()
    {
        // Arrange
        var repository = new FileSystemUserRepository(_usersDir);
        var user1 = new User(Guid.NewGuid(), "田中 太郎", "#FF0000", "user1.png");
        var user2 = new User(Guid.NewGuid(), "佐藤 次郎", "#00FF00", "user2.png");

        // Act
        await repository.SaveUserAsync(user1);
        await repository.SaveUserAsync(user2);
        var users = await repository.GetAllUsersAsync();

        // Assert
        Assert.AreEqual(2, users.Count());
        Assert.IsTrue(users.Any(u => u.Id == user1.Id));
        Assert.IsTrue(users.Any(u => u.Id == user2.Id));
    }
}
