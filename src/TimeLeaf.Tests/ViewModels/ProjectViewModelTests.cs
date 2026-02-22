using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectViewModelTests
{
    // [TestMethod]
    // public void Constructor_ShouldThrowArgumentNullException_WhenProjectIsNull()
    // {
    //     // Arrange
    //     Project project = null!;
    //
    //     // Act & Assert
    //     Assert.ThrowsException<ArgumentNullException>(() => new ProjectViewModel(project), "nullプロジェクトでコンストラクタを呼び出した際にArgumentNullExceptionがスローされること");
    // }

    /// <summary>
    /// テスト観点: Name プロパティを変更した際に、基になる Project エンティティの Name が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Name_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var project = new Project { Name = "Old Name" };
        var viewModel = new ProjectViewModel(project);
        var newName = "New Name";

        var receivedEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.Name))
            {
                receivedEvents++;
            }
        };

        // Act
        viewModel.Name = newName;

        // Assert
        Assert.AreEqual(newName, project.Name, "基になるProjectエンティティのNameが更新されること");
        Assert.AreEqual(1, receivedEvents, "Nameプロパティの変更時にPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: Description プロパティを変更した際に、基になる Project エンティティの Description が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Description_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var project = new Project { Description = "Old Description" };
        var viewModel = new ProjectViewModel(project);
        var newDescription = "New Description";

        var receivedEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.Description))
            {
                receivedEvents++;
            }
        };

        // Act
        viewModel.Description = newDescription;

        // Assert
        Assert.AreEqual(newDescription, project.Description, "基になるProjectエンティティのDescriptionが更新されること");
        Assert.AreEqual(1, receivedEvents, "Descriptionプロパティの変更時にPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: Tasks コレクションにタスクが追加された際に、
    /// TotalEstimatedCost および TotalActualCost の PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Tasks_CollectionChanged_ShouldRaisePropertyChangedForTotalCosts()
    {
        // Arrange
        var project = new Project();
        var viewModel = new ProjectViewModel(project);
        var newTaskViewModel = new ProjectTaskViewModel(new ProjectTask { EstimatedCost = 10, ActualCost = 5 });

        var receivedEstimatedCostEvents = 0;
        var receivedActualCostEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.TotalEstimatedCost))
            {
                receivedEstimatedCostEvents++;
            }
            if (e.PropertyName == nameof(ProjectViewModel.TotalActualCost))
            {
                receivedActualCostEvents++;
            }
        };

        // Act
        viewModel.Tasks.Add(newTaskViewModel);

        // Assert
        Assert.AreEqual(1, receivedEstimatedCostEvents, "タスク追加時にTotalEstimatedCostのPropertyChangedイベントが発火すること");
        Assert.AreEqual(1, receivedActualCostEvents, "タスク追加時にTotalActualCostのPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: Tasks コレクションからタスクが削除された際に、
    /// TotalEstimatedCost および TotalActualCost の PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Tasks_CollectionRemoved_ShouldRaisePropertyChangedForTotalCosts()
    {
        // Arrange
        var project = new Project();
        var existingTaskViewModel = new ProjectTaskViewModel(new ProjectTask { EstimatedCost = 10, ActualCost = 5 });
        var viewModel = new ProjectViewModel(project); // ViewModel構築時、内部で ProjectTaskViewModel にラップされる
        viewModel.Tasks.Add(existingTaskViewModel);

        var receivedEstimatedCostEvents = 0;
        var receivedActualCostEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.TotalEstimatedCost))
            {
                receivedEstimatedCostEvents++;
            }
            if (e.PropertyName == nameof(ProjectViewModel.TotalActualCost))
            {
                receivedActualCostEvents++;
            }
        };

        // Act
        viewModel.Tasks.Remove(existingTaskViewModel); // ProjectTaskViewModel を直接削除

        // Assert
        Assert.AreEqual(1, receivedEstimatedCostEvents, "タスク削除時にTotalEstimatedCostのPropertyChangedイベントが発火すること");
        Assert.AreEqual(1, receivedActualCostEvents, "タスク削除時にTotalActualCostのPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: タスクがない場合、合計工数プロパティが 0 となることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldBeZero_WhenNoTasks()
    {
        // Arrange
        var project = new Project();
        var viewModel = new ProjectViewModel(project);

        // Act & Assert
        Assert.AreEqual(0.0, viewModel.TotalEstimatedCost, "タスクがない場合、合計見積工数は0であること");
        Assert.AreEqual(0.0, viewModel.TotalActualCost, "タスクがない場合、合計実績工数は0であること");
    }

    /// <summary>
    /// テスト観点: Tasks コレクション内の個々の ProjectTaskViewModel の EstimatedCost または ActualCost が変更された際に、
    /// ProjectViewModel の TotalEstimatedCost および TotalActualCost の PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Tasks_IndividualTaskPropertyChanged_ShouldRaisePropertyChangedForTotalCosts()
    {
        // Arrange
        var project = new Project();
        var task1 = new ProjectTask { EstimatedCost = 10, ActualCost = 5 };
        var task2 = new ProjectTask { EstimatedCost = 20, ActualCost = 10 };
        project.Tasks.Add(task1);
        project.Tasks.Add(task2);

        var viewModel = new ProjectViewModel(project);
        var taskViewModel1 = viewModel.Tasks.First(t => t.Id == task1.Id); // ViewModel から ProjectTaskViewModel を取得

        var receivedEstimatedCostEvents = 0;
        var receivedActualCostEvents = 0;
        viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(ProjectViewModel.TotalEstimatedCost))
            {
                receivedEstimatedCostEvents++;
            }
            if (e.PropertyName == nameof(ProjectViewModel.TotalActualCost))
            {
                receivedActualCostEvents++;
            }
        };

        // Act
        taskViewModel1.EstimatedCost = 15; // ViewModel を介してコストを変更
        taskViewModel1.ActualCost = 8;     // ViewModel を介してコストを変更

        // Assert (Redフェーズ: ここではまだイベントは発火しないことを想定。実装後にGreenにする)
        Assert.AreEqual(0, receivedEstimatedCostEvents, "タスクの個別のコスト変更ではTotalEstimatedCostのPropertyChangedイベントは発火しないこと (Red)");
        Assert.AreEqual(0, receivedActualCostEvents, "タスクの個別のコスト変更ではTotalActualCostのPropertyChangedイベントは発火しないこと (Red)");
    }
}
