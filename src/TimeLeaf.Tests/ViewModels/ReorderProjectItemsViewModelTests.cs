using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ReorderProjectItemsViewModelTests
{
    [TestMethod]
    public void MoveItem_ShouldMoveItem_AndSyncUI()
    {
        // Arrange
        var parent = new ProjectContainer();
        var child1 = new ProjectTask();
        child1.UpdateName("Child 1");
        var child2 = new ProjectTask();
        child2.UpdateName("Child 2");
        parent.AddChild(child1);
        parent.AddChild(child2);

        // rootContainer として parent を渡す
        var viewModel = new ReorderProjectItemsViewModel(parent);

        // Items[0] は child1, Items[1] は child2 (parent 自身は Children に含まれないため)
        var child2Item = viewModel.Items.First(i => i.Item.Id == child2.Id);

        // Act
        viewModel.MoveItemCommand.Execute(new object[] { child2Item, true });

        // Assert
        Assert.AreEqual(2, viewModel.Items.Count); // 2 Children
        Assert.AreEqual(child2.Id, viewModel.Items[0].Item.Id);
        Assert.AreEqual(child1.Id, viewModel.Items[1].Item.Id);

        // ドメインモデルとの同期確認
        Assert.AreEqual(child2.Id, parent.Children.ElementAt(0).Id);
    }

    [TestMethod]
    public void CanMoveItem_ShouldReturnFalse_AtBoundaries()
    {
        // Arrange
        var parent = new ProjectContainer();
        var child1 = new ProjectTask();
        var child2 = new ProjectTask();
        parent.AddChild(child1);
        parent.AddChild(child2);

        var viewModel = new ReorderProjectItemsViewModel(parent);
        var item1 = viewModel.Items.First(i => i.Item.Id == child1.Id);
        var item2 = viewModel.Items.First(i => i.Item.Id == child2.Id);

        // Act & Assert
        // item1 は最上位なので上移動不可
        Assert.IsFalse(viewModel.MoveItemCommand.CanExecute(new object[] { item1, true }));
        // item1 は下移動可
        Assert.IsTrue(viewModel.MoveItemCommand.CanExecute(new object[] { item1, false }));

        // item2 は上移動可
        Assert.IsTrue(viewModel.MoveItemCommand.CanExecute(new object[] { item2, true }));
        // item2 は最下位なので下移動不可
        Assert.IsFalse(viewModel.MoveItemCommand.CanExecute(new object[] { item2, false }));
    }
}
