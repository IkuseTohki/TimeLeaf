using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.ViewModels;
using TimeLeaf.Models.Enums;

using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class AddTaskViewModelTests
{
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _userServiceMock = new Mock<IUserService>();
    }

    [TestMethod]
    public void ConfirmCommand_ShouldNotRequestClose_WhenNameIsWhiteSpace()
    {
        /*
        テスト観点: タスク名が空の時、RequestCloseイベントが呼ばれないことを確認する。
        */
        var viewModel = new AddTaskViewModel(_userServiceMock.Object);
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
        var viewModel = new AddTaskViewModel(_userServiceMock.Object);
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
        var viewModel = new AddTaskViewModel(_userServiceMock.Object);
        bool? closeResult = null;
        viewModel.RequestClose += (result) => closeResult = result;

        viewModel.CancelCommand.Execute(null);

        Assert.IsTrue(closeResult.HasValue);
        Assert.IsFalse(closeResult.Value);
    }

    [TestMethod]
    public void UserChanged_ShouldUpdateTeammatesCollection()
    {
        // 1. Arrange - テスト観点: IUserService.UserChanged が発生した際、Teammates 内の該当ユーザーが更新されること。
        var userId = Guid.NewGuid();
        var originalUser = new User(userId, "Original", "#000000", "");
        var updatedUser = new User(userId, "Updated", "#FFFFFF", "new.png");

        var viewModel = new AddTaskViewModel(_userServiceMock.Object);
        viewModel.Teammates = new ObservableCollection<User> { originalUser };

        // 2. Act - サービスからの変更通知をシミュレート
        _userServiceMock.Raise(s => s.UserChanged += null, updatedUser);

        // 3. Assert
        var result = viewModel.Teammates.First(u => u.Id == userId);
        Assert.AreEqual("Updated", result.DisplayName, "Teammates内のユーザー名が更新されていること");
        Assert.AreEqual("#FFFFFF", result.ThemeColor, "Teammates内のカラーが更新されていること");
    }

    [TestMethod]
    public void UserChanged_ShouldUpdateSelectedAssignee()
    {
        // 1. Arrange - テスト観点: 選択中の担当者が更新された場合、そのプロパティも最新の User インスタンスに差し替えられること。
        var userId = Guid.NewGuid();
        var originalUser = new User(userId, "Original", "#000000", "");
        var updatedUser = new User(userId, "Updated", "#FFFFFF", "new.png");

        var viewModel = new AddTaskViewModel(_userServiceMock.Object);
        viewModel.Assignee = originalUser;

        // 2. Act
        _userServiceMock.Raise(s => s.UserChanged += null, updatedUser);

        // 3. Assert
        Assert.AreEqual("Updated", viewModel.Assignee.DisplayName, "選択中の担当者情報が更新されていること");
        Assert.AreSame(updatedUser, viewModel.Assignee, "インスタンス自体が差し替えられていること");
    }
}
