using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Services;

[TestClass]
public class FileBasedIdentityServiceTests
{
    private string _tempDir = null!;
    private string _portableDir = null!;
    private string _homeDir = null!;

    private IIdentitySeedRepository _repository = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Initialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TimeLeafTests", Guid.NewGuid().ToString());
        _portableDir = Path.Combine(_tempDir, "App");
        _homeDir = Path.Combine(_tempDir, "Home");
        Directory.CreateDirectory(_portableDir);
        Directory.CreateDirectory(_homeDir);
        _repository = new FileIdentitySeedRepository(_portableDir, _homeDir);
        _userRepositoryMock = new Mock<IUserRepository>();
        _userServiceMock = new Mock<IUserService>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }

    /// <summary>
    /// テスト観点: どちらにもファイルがない場合、ホームディレクトリに新規作成されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task GetCurrentIdentity_ShouldCreateInHome_WhenNoFilesExist()
    {
        // Arrange
        var service = new FileBasedIdentityService(_repository, _userRepositoryMock.Object, _userServiceMock.Object);

        // Act
        var identity = await service.GetCurrentIdentityAsync();

        // Assert
        Assert.IsNotNull(identity);
        var expectedPath = Path.Combine(_homeDir, "seed.json");
        Assert.IsTrue(File.Exists(expectedPath));
        Assert.AreEqual(expectedPath, service.GetIdentityFilePath());
    }

    /// <summary>
    /// テスト観点: ポータブルディレクトリにファイルがある場合、それが最優先されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task GetCurrentIdentity_ShouldPreferPortable_WhenBothExist()
    {
        // Arrange
        var portableId = Guid.NewGuid();
        File.WriteAllText(
            Path.Combine(_portableDir, "seed.json"),
            $"{{\"Id\":\"{portableId}\", \"DisplayName\":\"PortableUser\"}}"
        );
        File.WriteAllText(
            Path.Combine(_homeDir, "seed.json"),
            $"{{\"Id\":\"{Guid.NewGuid()}\", \"DisplayName\":\"HomeUser\"}}"
        );

        var service = new FileBasedIdentityService(_repository, _userRepositoryMock.Object, _userServiceMock.Object);

        // Act
        var identity = await service.GetCurrentIdentityAsync();

        // Assert
        Assert.AreEqual(portableId, identity.Id);
        Assert.AreEqual(Path.Combine(_portableDir, "seed.json"), service.GetIdentityFilePath());
    }

    /// <summary>
    /// テスト観点: プロフィールの更新が、現在使用しているファイルに正しく保存されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task UpdateIdentity_ShouldSaveToCurrentFile()
    {
        // Arrange
        var service = new FileBasedIdentityService(_repository, _userRepositoryMock.Object, _userServiceMock.Object);
        await service.GetCurrentIdentityAsync(); // 新規作成（ここで1回目の保存が行われる）

        // Act
        await service.UpdateIdentityAsync("新しい名前", "#112233", "newicon.png");

        // Assert
        var identity = await service.GetCurrentIdentityAsync();
        Assert.AreEqual("新しい名前", identity.DisplayName);
        Assert.AreEqual("#112233", identity.ThemeColor);
        Assert.AreEqual("newicon.png", identity.IconPath);

        // 合計で2回（初期作成時 + 更新時）呼ばれていることを検証。
        // ※参照型のため引数の条件検証（DisplayName=="新しい名前"）を行うと、
        // 1回目の呼び出し時のインスタンスも検証時点では更新後の値になってしまっているため。
        _userRepositoryMock.Verify(r => r.SaveUserAsync(It.IsAny<User>()), Times.Exactly(2));
    }

    /// <summary>
    /// テスト観点: すでにリポジトリにプロフィールが存在する場合、デフォルト値ではなく保存されている内容がロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task GetCurrentIdentity_ShouldLoadExistingProfile_WhenRepositoryHasData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new User(userId, "保存された名前", "#FF0000", "saved_icon.png");

        // SeedリポジトリにはIDをセットしておく
        File.WriteAllText(Path.Combine(_homeDir, "seed.json"), $"{{\"Id\":\"{userId}\"}}");

        // Userリポジトリ（Mock）が既存ユーザーを返すように設定
        _userRepositoryMock.Setup(r => r.GetUserAsync(userId)).ReturnsAsync(existingUser);

        var service = new FileBasedIdentityService(_repository, _userRepositoryMock.Object, _userServiceMock.Object);

        // Act
        var identity = await service.GetCurrentIdentityAsync();

        // Assert
        Assert.AreEqual(userId, identity.Id);
        Assert.AreEqual(
            "保存された名前",
            identity.DisplayName,
            "リポジトリに保存されている名前がロードされるべきです。"
        );
        Assert.AreEqual("#FF0000", identity.ThemeColor);
        Assert.AreEqual("saved_icon.png", identity.IconPath);
    }
}
