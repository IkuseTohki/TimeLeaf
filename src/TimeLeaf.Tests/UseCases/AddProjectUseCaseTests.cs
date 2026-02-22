using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Threading.Tasks;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class AddProjectUseCaseTests
{
    /// <summary>
    /// テスト観点: AddProjectUseCase が、渡されたパラメータで Project を生成し、
    /// IProjectRepository の SaveAsync を正しく呼び出すことを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task ExecuteAsync_ShouldCreateAndSaveProject()
    {
        // Arrange
        var repositoryMock = new Mock<IProjectRepository>();
        var useCase = new AddProjectUseCase(repositoryMock.Object);

        var name = "Test Project";
        var description = "Test Description";
        var status = ProjectStatus.InProgress;
        var health = (ProjectHealth)3; // ProjectHealth.AtRisk の値 (例)

        // Act
        var createdProject = await useCase.ExecuteAsync(name, description, status, health);

        // Assert
        // 1. リポジトリの SaveAsync が1回だけ呼び出されたことを確認
        repositoryMock.Verify(r => r.SaveAsync(It.Is<Project>(p =>
            p.Name == name &&
            p.Description == description &&
            p.Status == status &&
            p.HealthStatus == health
        )), Times.Once);

        // 2. 返されたプロジェクトのプロパティが正しいことを確認
        Assert.IsNotNull(createdProject);
        Assert.AreEqual(name, createdProject.Name);
    }
}
