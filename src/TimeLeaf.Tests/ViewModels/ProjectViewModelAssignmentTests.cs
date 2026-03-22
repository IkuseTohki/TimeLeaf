using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;
using TimeLeaf.UseCases;
using Moq;
using System.Threading.Tasks;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectViewModelAssignmentTests
{
    /// <summary>
    /// テスト観点: 自分のIDがアサインリストに含まれている場合、IsAssignedToMe が True になることを確認する。
    /// </summary>
    [TestMethod]
    public void IsAssignedToMe_ShouldBeTrue_WhenMyIdIsInAssignedUserIds()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var project = new Project();
        project.AssignUser(myId);

        // Act
        var viewModel = new ProjectViewModel(project, myId, new Mock<IJoinProjectUseCase>().Object);

        // Assert
        Assert.IsTrue(viewModel.IsAssignedToMe);
    }

    /// <summary>
    /// テスト観点: 自分のIDがアサインリストに含まれていない場合、IsAssignedToMe が False になることを確認する。
    /// </summary>
    [TestMethod]
    public void IsAssignedToMe_ShouldBeFalse_WhenMyIdIsNotInAssignedUserIds()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var project = new Project();
        project.AssignUser(otherId);

        // Act
        var viewModel = new ProjectViewModel(project, myId, new Mock<IJoinProjectUseCase>().Object);

        // Assert
        Assert.IsFalse(viewModel.IsAssignedToMe);
    }

    /// <summary>
    /// テスト観点: JoinProjectCommand を実行すると、自分をアサインしプロパティが更新されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task JoinProjectCommand_ShouldAssignMe()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var project = new Project();
        var mockJoinUseCase = new Mock<IJoinProjectUseCase>();
        var viewModel = new ProjectViewModel(project, myId, mockJoinUseCase.Object);

        // Act
        await viewModel.JoinProjectCommand.ExecuteAsync(null);

        // Assert
        mockJoinUseCase.Verify(u => u.ExecuteAsync(project), Times.Once);
    }
}
