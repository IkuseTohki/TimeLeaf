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
        var project = new Project();
        project.UpdateName("Old Name");
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
        var project = new Project();
        project.UpdateDescription("Old Description");
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
        project.AddTask(newTaskViewModel.Model);
        viewModel.SyncFromModel(); // 手動同期が必要になった

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
        project.RemoveTask(existingTaskViewModel.Id); // IDで削除
        viewModel.SyncFromModel(); // 手動同期

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
        project.AddTask(task1);
        project.AddTask(task2);

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

        // Assert
        // EstimatedCost で 1回、ActualCost で 1回、合計 2回ずつ発火するはず
        Assert.AreEqual(2, receivedEstimatedCostEvents, "タスクの個別のコスト変更によりTotalEstimatedCostのPropertyChangedイベントが発火すること");
        Assert.AreEqual(2, receivedActualCostEvents, "タスクの個別のコスト変更によりTotalActualCostのPropertyChangedイベントが発火すること");
    }

    /// <summary>
    /// テスト観点: タスクを追加した際に、ViewModelのTasksコレクションに重複して追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldAddOnlyOneViewModel()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project);

        // Act
        // Model への直接追加後、ViewModel を同期する
        project.AddTask(new ProjectTask { Name = "New Task" });
        projectViewModel.SyncFromModel();

        // Assert
        Assert.AreEqual(1, project.Tasks.Count, "Modelのタスク数が1であること");
        Assert.AreEqual(1, projectViewModel.Tasks.Count, "ProjectViewModelのタスク数が1であること");
    }

    /// <summary>
    /// テスト観点: Model.Tasks.Clear() (Resetアクション) が発生した際に、
    /// ViewModelのTasksコレクションも正しくクリアされることを確認する。
    /// </summary>
    [TestMethod]
    public void ModelClear_ShouldClearViewModelTasks()
    {
        // Arrange
        var project = new Project();
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(project);
        project.AddTask(new ProjectTask { Name = "Existing Task" });
        projectViewModel.SyncFromModel();

        // この時点で ViewModel.Tasks には1つ入っているはず
        Assert.AreEqual(1, projectViewModel.Tasks.Count, "初期状態でVMのタスクが1つであること");

        // Act
        project.ClearTasks();
        projectViewModel.SyncFromModel();

        // Assert
        Assert.AreEqual(0, project.Tasks.Count, "Modelのタスクがクリアされていること");
        Assert.AreEqual(0, projectViewModel.Tasks.Count, "Model.Clear() 後に ViewModel のタスクもクリアされていること");
    }

    /// <summary>
    /// テスト観点: DisplayLastUpdated プロパティが、現在時刻からの経過時間の境界値において
    /// 適切な相対時間文字列を返すことを確認する。
    /// </summary>
    [TestMethod]
    [DataRow(0, "たった今")]
    [DataRow(59, "たった今")]
    [DataRow(60, "1分前")]
    [DataRow(119, "1分前")]
    [DataRow(120, "2分前")]
    [DataRow(3599, "59分前")]
    [DataRow(3600, "1時間前")]
    [DataRow(86399, "23時間前")]
    public void DisplayLastUpdated_ShouldReturnRelativeTimeStrings_AtBoundaries(int secondsOffset, string expected)
    {
        // Arrange
        // DateTime.Now の微細な Ticks による誤差を防ぐため、秒単位で丸める
        var now = DateTime.Now;
        var baseTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
        var project = new Project { UpdatedAt = baseTime.AddSeconds(-secondsOffset) };
        var viewModel = new ProjectViewModel(project);

        // Act
        var actual = viewModel.DisplayLastUpdated;

        // Assert
        Assert.AreEqual(expected, actual, $"Offset {secondsOffset}s should result in '{expected}'");
    }

    /// <summary>
    /// テスト観点: ちょうど24時間経過したタイミングで、相対表示から絶対日付表示に切り替わることを確認する。
    /// </summary>
    [TestMethod]
    public void DisplayLastUpdated_ShouldSwitchToFullDate_At24HoursBoundary()
    {
        // Arrange
        var targetDate = new DateTime(2026, 1, 1, 12, 34, 0);
        var project = new Project { UpdatedAt = targetDate };
        var viewModel = new ProjectViewModel(project);

        // Act
        var actual = viewModel.DisplayLastUpdated;

        // Assert
        Assert.AreEqual("2026/01/01 12:34", actual);
    }
}
