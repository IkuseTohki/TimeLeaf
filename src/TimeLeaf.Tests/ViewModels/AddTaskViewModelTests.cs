using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.ViewModels;
using TimeLeaf.Models.Enums;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class AddTaskViewModelTests
{
    [TestMethod]
    public void ConfirmCommand_ShouldNotRequestClose_WhenNameIsWhiteSpace()
    {
        /*
        テスト観点: タスク名が空の時、RequestCloseイベントが呼ばれないことを確認する。
        */
        var viewModel = new AddTaskViewModel();
        bool isCloseRequested = false;
        viewModel.RequestClose += (result) => isCloseRequested = true;

        viewModel.Name = "  ";
        viewModel.ConfirmCommand.Execute(null);

        Assert.IsFalse(isCloseRequested, "名前が空白の場合、ダイアログを閉じてはいけません。");
    }

    [TestMethod]
    public void ConfirmCommand_ShouldRequestCloseWithTrue_WhenNameIsValid()
    {
        /*
        テスト観点: 正しいタスク名が入力された時、RequestClose(true)が呼ばれることを確認する。
        */
        var viewModel = new AddTaskViewModel();
        bool? closeResult = null;
        viewModel.RequestClose += (result) => closeResult = result;

        viewModel.Name = "New Task";
        viewModel.ConfirmCommand.Execute(null);

        Assert.IsTrue(closeResult.HasValue, "名前が有効な場合、ダイアログを閉じる要求が必要です。");
        Assert.IsTrue(closeResult.Value, "名前が有効な場合、RequestClose(true) である必要があります。");
    }

    [TestMethod]
    public void CancelCommand_ShouldRequestCloseWithFalse()
    {
        /*
        テスト観点: キャンセルされた時、RequestClose(false)が呼ばれることを確認する。
        */
        var viewModel = new AddTaskViewModel();
        bool? closeResult = null;
        viewModel.RequestClose += (result) => closeResult = result;

        viewModel.CancelCommand.Execute(null);

        Assert.IsTrue(closeResult.HasValue);
        Assert.IsFalse(closeResult.Value);
    }
}
