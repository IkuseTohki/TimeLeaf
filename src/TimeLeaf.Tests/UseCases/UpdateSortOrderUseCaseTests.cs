using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;

namespace TimeLeaf.Tests.UseCases;

[TestClass]
public class UpdateSortOrderUseCaseTests
{
    private Mock<ISaveProjectUseCase> _mockSaveUseCase = null!;
    private UpdateSortOrderUseCase _useCase = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mockSaveUseCase = new Mock<ISaveProjectUseCase>();
        // 実装クラスはまだ未定義なので、ここではコンパイルエラーになるはず（Red）
        _useCase = new UpdateSortOrderUseCase(_mockSaveUseCase.Object);
    }

    /// <summary>
    /// テスト観点: 渡された順序マップに従って、タスクとコンテナの SortOrder が正しく更新されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ExecuteAsync_ShouldReorderProjectCollectionsAndSave()
    {
        // Arrange
        var project = new Project(Guid.NewGuid());
        var t1 = new ProjectTask { Id = Guid.NewGuid() };
        var t2 = new ProjectTask { Id = Guid.NewGuid() };
        project.AddTask(t1);
        project.AddTask(t2);

        // 逆順のリスト
        var orderedIds = new List<Guid> { t2.Id, t1.Id };

        // Act
        await _useCase.ExecuteAsync(project, orderedIds);

        // Assert
        Assert.AreEqual(t2.Id, project.Tasks[0].Id);
        Assert.AreEqual(t1.Id, project.Tasks[1].Id);
        _mockSaveUseCase.Verify(s => s.ExecuteAsync(project), Times.Once);
    }
}
