using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class AddContainerUseCaseTests
{
    private Mock<ISaveProjectUseCase> _mockSaveUseCase = null!;
    private Mock<ILogger<AddContainerUseCase>> _mockLogger = null!;
    private AddContainerUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mockSaveUseCase = new Mock<ISaveProjectUseCase>();
        _mockLogger = new Mock<ILogger<AddContainerUseCase>>();
        _useCase = new AddContainerUseCase(_mockSaveUseCase.Object, _mockLogger.Object);
    }

    /// <summary>
    /// テスト観点: 正常なパラメータでコンテナがルートに追加されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithValidRootContainer_ShouldAddAndSave()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var name = "Root Group";

        // Act
        await _useCase.ExecuteAsync(project, name, "Description", null);

        // Assert
        var container = project.Containers.FirstOrDefault(c => c.Name == name);
        Assert.IsNotNull(container);
        Assert.IsNull(container.ParentId);
        _mockSaveUseCase.Verify(s => s.ExecuteAsync(project), Times.Once);
    }

    /// <summary>
    /// テスト観点: 親IDを指定した場合、ネストされたコンテナとして追加されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_WithParentId_ShouldSetParentId()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var parentId = Guid.NewGuid();

        // Act
        await _useCase.ExecuteAsync(project, "Sub Group", "", parentId);

        // Assert
        var container = project.Containers.First();
        Assert.AreEqual(parentId, container.ParentId);
    }

    /// <summary>
    /// テスト観点: 名前が空または空白の場合、ArgumentException がスローされることを確認する。
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(null)]
    public async Task ExecuteAsync_WithInvalidName_ShouldThrowException(string? invalidName)
    {
        // Arrange
        var project = new Project(Guid.NewGuid());

        // Act & Assert
        try
        {
            await _useCase.ExecuteAsync(project, invalidName!, "", null);
            Assert.Fail("Exception should have been thrown.");
        }
        catch (ArgumentException)
        {
            // Success
        }
    }
}
