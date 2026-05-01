using System;
using System.Threading.Tasks;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;

namespace TimeLeaf.UseCases.Tests;

[TestClass]
public class FindProjectUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;
    private FindProjectUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _useCase = new FindProjectUseCase(_projectServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenProjectExists_ReturnsProject()
    {
        // Arrange
        /* テスト観点: 指定されたIDでプロジェクトが存在する場合、Serviceから取得したProjectオブジェクトが返されることを確認する。 */
        var projectId = Guid.NewGuid();
        var expectedProject = new Project(Guid.Empty) { Id = projectId };
        expectedProject.UpdateName("Test Project");
        _projectServiceMock.Setup(s => s.GetProjectAsync(projectId)).ReturnsAsync(expectedProject);

        // Act
        var result = await _useCase.ExecuteAsync(projectId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedProject, result);
        _projectServiceMock.Verify(s => s.GetProjectAsync(projectId), Times.Once);
    }

    [TestMethod]
    public async Task ExecuteAsync_WhenProjectDoesNotExist_ReturnsNull()
    {
        // Arrange
        /* テスト観点: 指定されたIDでプロジェクトが存在しない場合、nullが返されることを確認する。 */
        var projectId = Guid.NewGuid();
        _projectServiceMock.Setup(s => s.GetProjectAsync(projectId)).ReturnsAsync((Project?)null);

        // Act
        var result = await _useCase.ExecuteAsync(projectId);

        // Assert
        Assert.IsNull(result);
        _projectServiceMock.Verify(s => s.GetProjectAsync(projectId), Times.Once);
    }
}
