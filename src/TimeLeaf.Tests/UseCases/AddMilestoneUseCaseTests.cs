using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class AddMilestoneUseCaseTests
{
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
    }

    /// <summary>
    /// テスト観点: AddMilestoneUseCase が、指定された内容でマイルストーンを作成し、
    /// プロジェクトに追加した上で、プロジェクトを保存することを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldAddMilestoneAndSaveProject()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");

        var useCase = new AddMilestoneUseCase(_saveUseCaseMock.Object);
        var date = DateTime.Today;
        var label = "Test Milestone";

        // Act
        await useCase.ExecuteAsync(project, date, label);

        // Assert
        // 1. プロジェクトにマイルストーンが追加されていること
        Assert.AreEqual(1, project.Milestones.Count());
        var added = project.Milestones.First();
        Assert.AreEqual(date, added.Date);
        Assert.AreEqual(label, added.Label);

        // 2. プロジェクトの保存が呼び出されていること
        _saveUseCaseMock.Verify(s => s.ExecuteAsync(project), Times.Once);
    }
}
