using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
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

    [TestInitialize]
    public void Initialize()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TimeLeafTests", Guid.NewGuid().ToString());
        _portableDir = Path.Combine(_tempDir, "App");
        _homeDir = Path.Combine(_tempDir, "Home");
        Directory.CreateDirectory(_portableDir);
        Directory.CreateDirectory(_homeDir);
        _repository = new FileIdentitySeedRepository(_portableDir, _homeDir);
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
        var service = new FileBasedIdentityService(_repository);

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
        File.WriteAllText(Path.Combine(_portableDir, "seed.json"), $"{{\"Id\":\"{portableId}\", \"DisplayName\":\"PortableUser\"}}");
        File.WriteAllText(Path.Combine(_homeDir, "seed.json"), $"{{\"Id\":\"{Guid.NewGuid()}\", \"DisplayName\":\"HomeUser\"}}");

        var service = new FileBasedIdentityService(_repository);

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
        var service = new FileBasedIdentityService(_repository);
        await service.GetCurrentIdentityAsync(); // 新規作成（Home）

        // Act
        await service.UpdateIdentityAsync("新しい名前", "#112233", "newicon.png");

        // Assert
        var identity = await service.GetCurrentIdentityAsync();
        Assert.AreEqual("新しい名前", identity.DisplayName);
        Assert.AreEqual("#112233", identity.ThemeColor);
        Assert.AreEqual("newicon.png", identity.IconPath);
    }
}
