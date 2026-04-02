using System;
using System.Linq;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases.Tests;

[TestClass]
public class CheckAssignmentUseCaseTests
{
    private Mock<IIdentityService> _identityServiceMock = null!;
    private CheckAssignmentUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _identityServiceMock = new Mock<IIdentityService>();
        _useCase = new CheckAssignmentUseCase(_identityServiceMock.Object);
    }

    [TestMethod]
    public void IsUserAssigned_WhenUserIsAssigned_ReturnsTrue()
    {
        // Arrange
        /* テスト観点: ユーザーがプロジェクトにアサインされている場合、IsUserAssignedがtrueを返すことを確認する。 */
        var userId = Guid.NewGuid();
        var project = new Project();
        project.UpdateName("Test Project");
        project.AssignUser(userId);

        _identityServiceMock.Setup(i => i.CurrentUserId).Returns(userId);

        // Act
        var result = _useCase.IsUserAssigned(project);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsUserAssigned_WhenUserIsNotAssigned_ReturnsFalse()
    {
        // Arrange
        /* テスト観点: ユーザーがプロジェクトにアサインされていない場合、IsUserAssignedがfalseを返すことを確認する。 */
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var project = new Project();
        project.UpdateName("Test Project");
        project.AssignUser(otherUserId);

        _identityServiceMock.Setup(i => i.CurrentUserId).Returns(userId);

        // Act
        var result = _useCase.IsUserAssigned(project);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IsUserAssigned_WhenNoOneIsAssigned_ReturnsFalse()
    {
        // Arrange
        /* テスト観点: プロジェクトに誰もアサインされていない場合、IsUserAssignedがfalseを返すことを確認する。 */
        var userId = Guid.NewGuid();
        var project = new Project();
        project.UpdateName("Test Project");

        _identityServiceMock.Setup(i => i.CurrentUserId).Returns(userId);

        // Act
        var result = _useCase.IsUserAssigned(project);

        // Assert
        Assert.IsFalse(result);
    }
}
