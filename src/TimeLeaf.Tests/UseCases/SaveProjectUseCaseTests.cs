using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SaveProjectUseCaseTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
    }

    /// <summary>
    /// テスト観点: 単一プロジェクト保存ユースケースがリポジトリの SaveAsync を正しく呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldCallSaveOnRepository()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Single Project");
        var useCase = new SaveProjectUseCase(_repositoryMock.Object);

        // Act
        await useCase.ExecuteAsync(project);

        // Assert
        _repositoryMock.Verify(r => r.SaveAsync(project), Times.Once);
    }
}
