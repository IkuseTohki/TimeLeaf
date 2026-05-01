using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class JoinProjectUseCaseTests
{
    /// <summary>
    /// テスト観点: プロジェクトに自分をアサインし、保存されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task Execute_ShouldAssignMeToProjectAndSave()
    {
        // Arrange
        var myId = Guid.NewGuid();
        var project = new Project(Guid.Empty);

        var mockIdentityService = new Mock<IIdentityService>();
        mockIdentityService.Setup(s => s.CurrentUserId).Returns(myId);

        var mockSaveUseCase = new Mock<ISaveProjectUseCase>();

        var useCase = new JoinProjectUseCase(mockIdentityService.Object, mockSaveUseCase.Object);

        // Act
        await useCase.ExecuteAsync(project);

        // Assert
        Assert.IsTrue(project.AssignedUserIds.Contains(myId));
        mockSaveUseCase.Verify(s => s.ExecuteAsync(project), Times.Once);
    }
}
