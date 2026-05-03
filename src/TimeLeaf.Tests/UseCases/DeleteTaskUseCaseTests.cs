using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class DeleteTaskUseCaseTests
{
    private Mock<IProjectService> _projectServiceMock = null!;
    private DeleteTaskUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _projectServiceMock = new Mock<IProjectService>();
        _useCase = new DeleteTaskUseCase(_projectServiceMock.Object);
    }

    [TestMethod]
    public async Task ExecuteAsync_ShouldDelegateToDeleteTaskAsync()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var taskId = Guid.NewGuid();

        // Act
        await _useCase.ExecuteAsync(project, taskId);

        // Assert
        _projectServiceMock.Verify(
            x => x.DeleteTaskAsync(project, taskId),
            Times.Once,
            "ProjectService.DeleteTaskAsync が呼び出されていること"
        );
    }
}
