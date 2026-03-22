using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProfileEditViewModelTests
{
    /// <summary>
    /// テスト観点: 初期化時に現在のアイデンティティがロードされることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Initialize_ShouldLoadCurrentIdentity()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var myIdentity = new User(myId, "初期名", "#FF0000", "old.png");

        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.GetCurrentIdentityAsync()).ReturnsAsync(myIdentity);

        // Act
        var viewModel = new ProfileEditViewModel(mockIdentityService.Object);
        await viewModel.LoadAsync();

        // Assert
        Assert.AreEqual("初期名", viewModel.DisplayName);
        Assert.AreEqual("#FF0000", viewModel.ThemeColor);
        Assert.AreEqual("old.png", viewModel.IconPath);
    }

    /// <summary>
    /// テスト観点: 保存を実行すると IdentityService が更新されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Save_ShouldUpdateIdentityService()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var myIdentity = new User(myId, "初期名", "#FF0000", "old.png");
        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.GetCurrentIdentityAsync()).ReturnsAsync(myIdentity);

        var viewModel = new ProfileEditViewModel(mockIdentityService.Object);
        await viewModel.LoadAsync();

        // Act
        viewModel.DisplayName = "新しい名前";
        viewModel.ThemeColor = "#00FF00";
        await viewModel.SaveCommand.ExecuteAsync(null);

        // Assert
        mockIdentityService.Verify(s => s.UpdateIdentityAsync("新しい名前", "#00FF00", "old.png"), Times.Once);
    }
}
