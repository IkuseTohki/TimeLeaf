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
public class AddCommentUseCaseTests
{
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private readonly Guid _testUserId = Guid.NewGuid();

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _identityServiceMock = new Mock<IIdentityService>();
        _identityServiceMock.Setup(u => u.CurrentUserId).Returns(_testUserId);
    }

    /// <summary>
    /// テスト観点: AddCommentUseCase が、指定された内容でコメントを作成し、
    /// タスクに追加した上で、プロジェクトを保存することを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldAddCommentAndSaveProject()
    {
        // Arrange
        var project = new Project();
        var task = new ProjectTask();
        project.AddTask(task);

        var useCase = new AddCommentUseCase(_saveUseCaseMock.Object, _identityServiceMock.Object);
        var content = "Test Comment";

        // Act
        await useCase.ExecuteAsync(project, task, content);

        // Assert
        // 1. タスクにコメントが追加されていること
        Assert.AreEqual(1, task.Comments.Count());
        var addedComment = task.Comments.First();
        Assert.AreEqual(content, addedComment.Content);
        Assert.AreEqual(_testUserId, addedComment.AuthorId);

        // 2. プロジェクトの保存が呼び出されていること
        _saveUseCaseMock.Verify(s => s.ExecuteAsync(project), Times.Once);
    }
}
