using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class AddContainerViewModelTests
{
    /// <summary>
    /// テスト観点: 名前が入力されている場合、確定コマンドで RequestClose(true) が呼ばれることを確認する。
    /// </summary>
    [TestMethod]
    public void ConfirmCommand_WithValidName_ShouldRaiseRequestCloseWithTrue()
    {
        // Arrange
        var vm = new AddContainerViewModel();
        vm.Name = "New Group";
        bool? result = null;
        vm.RequestClose += (r) => result = r;

        // Act
        vm.ConfirmCommand.Execute(null);

        // Assert
        Assert.IsTrue(result == true);
    }

    /// <summary>
    /// テスト観点: 名前が空の場合、確定コマンドを実行しても RequestClose が呼ばれないことを確認する。
    /// </summary>
    [TestMethod]
    public void ConfirmCommand_WithEmptyName_ShouldNotRaiseRequestClose()
    {
        // Arrange
        var vm = new AddContainerViewModel();
        vm.Name = "";
        bool raised = false;
        vm.RequestClose += (r) => raised = true;

        // Act
        vm.ConfirmCommand.Execute(null);

        // Assert
        Assert.IsFalse(raised);
    }

    /// <summary>
    /// テスト観点: キャンセルコマンドで RequestClose(false) が呼ばれることを確認する。
    /// </summary>
    [TestMethod]
    public void CancelCommand_ShouldRaiseRequestCloseWithFalse()
    {
        // Arrange
        var vm = new AddContainerViewModel();
        bool? result = null;
        vm.RequestClose += (r) => result = r;

        // Act
        vm.CancelCommand.Execute(null);

        // Assert
        Assert.IsTrue(result == false);
    }
}
