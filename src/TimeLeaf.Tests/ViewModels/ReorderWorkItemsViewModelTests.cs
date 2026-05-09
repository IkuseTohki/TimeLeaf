using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ReorderWorkItemsViewModelTests
{
    private Mock<IUpdateSortOrderUseCase> _mockUseCase = null!;
    private Project _project = null!;

    [TestInitialize]
    public void Initialize()
    {
        _mockUseCase = new Mock<IUpdateSortOrderUseCase>();
        _project = new Project(Guid.NewGuid());
    }

    /// <summary>
    /// テスト観点: 初期化時にプロジェクト内のアイテムがコレクションにロードされることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldLoadItemsFromProject()
    {
        // Arrange
        var t1 = new ProjectTask();
        t1.UpdateName("Task 1");
        var c1 = new ProjectContainer(Guid.NewGuid(), "Group 1");

        var items = new List<ProjectWorkItem> { t1, c1 };

        // Act
        var vm = new ReorderWorkItemsViewModel(_project, items, _mockUseCase.Object);

        // Assert
        Assert.AreEqual(2, vm.Items.Count);
        Assert.AreEqual("Task 1", vm.Items[0].Name);
    }

    /// <summary>
    /// テスト観点: MoveUp コマンドによってコレクション内の順序が入れ替わることを確認する。
    /// </summary>
    [TestMethod]
    public void MoveUpCommand_ShouldSwapItems()
    {
        // Arrange
        var item1 = new ProjectTask();
        item1.UpdateName("Item 1");
        var item2 = new ProjectTask();
        item2.UpdateName("Item 2");
        var items = new List<ProjectWorkItem> { item1, item2 };
        var vm = new ReorderWorkItemsViewModel(_project, items, _mockUseCase.Object);
        vm.SelectedItem = vm.Items[1]; // Item 2 を選択

        // Act
        vm.MoveUpCommand.Execute(null);

        // Assert
        Assert.AreEqual("Item 2", vm.Items[0].Name);
        Assert.AreEqual("Item 1", vm.Items[1].Name);
    }

    /// <summary>
    /// テスト観点: 確定コマンド実行時にユースケースが正しい順序マップで呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task ConfirmCommand_ShouldCallUseCaseWithCorrectOrders()
    {
        // Arrange
        var item1 = new ProjectTask();
        item1.UpdateName("A");
        var item2 = new ProjectTask();
        item2.UpdateName("B");
        var items = new List<ProjectWorkItem> { item1, item2 };
        var vm = new ReorderWorkItemsViewModel(_project, items, _mockUseCase.Object);

        // Act: 順序を入れ替えて確定
        vm.SelectedItem = vm.Items[1]; // B を選択
        vm.MoveUpCommand.Execute(null); // [B, A] の順になる
        vm.ConfirmCommand.Execute(null);

        // Assert
        _mockUseCase.Verify(
            u =>
                u.ExecuteAsync(
                    _project,
                    It.Is<IEnumerable<Guid>>(ids => ids.First() == item2.Id && ids.Last() == item1.Id)
                ),
            Times.Once
        );
    }
}
