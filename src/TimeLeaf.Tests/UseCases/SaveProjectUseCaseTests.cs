using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class SaveProjectUseCaseTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");
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
        var useCase = new SaveProjectUseCase(_repositoryMock.Object, _userServiceMock.Object);

        // Act
        await useCase.ExecuteAsync(project);

        // Assert
        _repositoryMock.Verify(r => r.SaveAsync(project, "test-user"), Times.Once);
    }

    /// <summary>
    /// テスト観点: 複数プロジェクト保存ユースケースがリポジトリの SaveAllAsync を正しく呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_WithMultipleProjects_ShouldCallSaveAllOnRepository()
    {
        // Arrange
        var project1 = new Project();
        project1.UpdateName("P1");
        var project2 = new Project();
        project2.UpdateName("P2");
        var projects = new[] { project1, project2 };

        var useCase = new SaveProjectUseCase(_repositoryMock.Object, _userServiceMock.Object);

        // Act
        // 注意: この時点ではコンパイルエラーになる可能性があります（Red）
        await ((ISaveProjectUseCase)useCase).ExecuteAsync(projects);

        // Assert
        _repositoryMock.Verify(r => r.SaveAllAsync(projects, "test-user"), Times.Once);
    }
}
