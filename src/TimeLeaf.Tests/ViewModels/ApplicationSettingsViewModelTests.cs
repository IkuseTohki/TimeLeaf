using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models;
using TimeLeaf.Repositories;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ApplicationSettingsViewModelTests
{
    private Mock<IApplicationSettingsRepository> _settingsRepoMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _settingsRepoMock = new Mock<IApplicationSettingsRepository>();
    }

    [TestMethod]
    public void Constructor_ShouldLoadSettings()
    {
        /*
        テスト観点: コンストラクタで現在の設定がViewModelのプロパティに正しくロードされることを確認する。
        */
        var settings = new ApplicationSettings
        {
            StoragePath = @"C:\Data",
            Theme = "Light",
            EnableOsNotification = false,
            EnableAppNotification = false,
            MinimizeOnClose = false,
        };

        var viewModel = new ApplicationSettingsViewModel(_settingsRepoMock.Object, settings);

        Assert.AreEqual(settings.StoragePath, viewModel.StoragePath);
        Assert.AreEqual(settings.Theme, viewModel.SelectedTheme);
        Assert.AreEqual(settings.EnableOsNotification, viewModel.EnableOsNotification);
        Assert.AreEqual(settings.EnableAppNotification, viewModel.EnableAppNotification);
        Assert.AreEqual(settings.MinimizeOnClose, viewModel.MinimizeOnClose);
    }

    [TestMethod]
    public void SaveCommand_ShouldUpdateModelAndCallRepository()
    {
        /*
        テスト観点: Saveコマンド実行時、ViewModelのプロパティがModelに反映され、リポジトリのSaveが呼ばれることを確認する。
        */
        var settings = new ApplicationSettings();
        var viewModel = new ApplicationSettingsViewModel(_settingsRepoMock.Object, settings);

        viewModel.SelectedTheme = "Dark";
        viewModel.EnableOsNotification = true;
        viewModel.EnableAppNotification = true;
        viewModel.MinimizeOnClose = false;

        viewModel.SaveCommand.Execute(null);

        Assert.AreEqual("Dark", settings.Theme);
        Assert.IsTrue(settings.EnableOsNotification);
        Assert.IsTrue(settings.EnableAppNotification);
        Assert.IsFalse(settings.MinimizeOnClose);
        _settingsRepoMock.Verify(r => r.Save(settings), Times.Once);
    }
}
