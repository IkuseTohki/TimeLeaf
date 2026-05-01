using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectViewModelTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _userServiceMock = new Mock<IUserService>();

        // ProjectViewModel がタスクを生成する際に使用するファクトリのセットアップ
        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));
    }

    // --- 既存のテストケース (Name_ShouldUpdateModelAndRaisePropertyChanged 等) ---
    // (省略)

    // --- AssignmentTests から移植 ---
    [TestMethod]
    public void IsAssignedToMe_ShouldBeTrue_WhenMyIdIsInAssignedUserIds()
    {
        var myId = Guid.NewGuid();
        var project = new Project(Guid.Empty);
        project.AssignUser(myId);

        var viewModel = new ProjectViewModel(
            project,
            myId,
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        Assert.IsTrue(viewModel.IsAssignedToMe);
    }

    [TestMethod]
    public void IsAssignedToMe_ShouldBeFalse_WhenMyIdIsNotInAssignedUserIds()
    {
        var myId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var project = new Project(Guid.Empty);
        project.AssignUser(otherId);

        var viewModel = new ProjectViewModel(
            project,
            myId,
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        Assert.IsFalse(viewModel.IsAssignedToMe);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task JoinProjectCommand_ShouldAssignMe()
    {
        var myId = Guid.NewGuid();
        var project = new Project(Guid.Empty);
        var mockJoinUseCase = new Mock<IJoinProjectUseCase>();
        var viewModel = new ProjectViewModel(project, myId, mockJoinUseCase.Object, _viewModelFactoryMock.Object);

        await viewModel.JoinProjectCommand.ExecuteAsync(null);

        mockJoinUseCase.Verify(u => u.ExecuteAsync(project), Times.Once);
    }

    // --- SyncTests から移植 ---
    [TestMethod]
    public void ProjectViewModel_UpdateProperty_ShouldUpdateModel()
    {
        var project = new Project(Guid.Empty);
        project.UpdateName("Old Name");
        project.UpdateDescription("Old Desc");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        viewModel.Name = "New Name";
        viewModel.Description = "New Desc";

        Assert.AreEqual("New Name", project.Name);
        Assert.AreEqual("New Desc", project.Description);
    }

    [TestMethod]
    public void ProjectTaskViewModel_UpdateProperty_ShouldUpdateModel()
    {
        var task = new ProjectTask();
        task.UpdateName("Old Task");
        task.AssignTo("Old User");
        var viewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);

        viewModel.Name = "New Task";
        viewModel.Assignee = "New User";

        Assert.AreEqual("New Task", task.Name);
        Assert.AreEqual("New User", task.Assignee);
    }

    [TestMethod]
    public void ProjectModel_AddTask_ShouldReflectInViewModel()
    {
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var newTask = new ProjectTask();
        newTask.UpdateName("New Task");

        project.AddTask(newTask);
        viewModel.SyncFromModel();

        Assert.IsNotNull(viewModel.Tasks);
        Assert.AreEqual(1, viewModel.Tasks.Count);
        Assert.AreEqual(newTask.Id, viewModel.Tasks.First().Id);
    }

    [TestMethod]
    public void ProjectModel_RemoveTask_ShouldReflectInViewModel()
    {
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        var task = new ProjectTask();
        task.UpdateName("Task 1");
        project.AddTask(task);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        project.RemoveTask(task.Id);
        viewModel.SyncFromModel();

        Assert.IsNotNull(viewModel.Tasks);
        Assert.AreEqual(0, viewModel.Tasks.Count);
    }

    /// <summary>
    /// テスト観点: Name プロパティを変更した際に、基になる Project エンティティの Name が更新され、
    /// かつ PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Name_ShouldUpdateModelAndRaisePropertyChanged()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        project.UpdateName("Old Name");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
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
        var project = new Project(Guid.Empty);
        project.UpdateDescription("Old Description");
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
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
        Assert.AreEqual(
            newDescription,
            project.Description,
            "基になるProjectエンティティのDescriptionが更新されること"
        );
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
        var project = new Project(Guid.Empty);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var task = new ProjectTask();
        task.UpdateEstimatedCost(10);
        task.UpdateActualCost(5);
        var newTaskViewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);

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
        viewModel.SyncFromModel();

        // Assert
        Assert.AreEqual(
            1,
            receivedEstimatedCostEvents,
            "タスク追加時にTotalEstimatedCostのPropertyChangedイベントが発火すること"
        );
        Assert.AreEqual(
            1,
            receivedActualCostEvents,
            "タスク追加時にTotalActualCostのPropertyChangedイベントが発火すること"
        );
    }

    /// <summary>
    /// テスト観点: Tasks コレクションからタスクが削除された際に、
    /// TotalEstimatedCost および TotalActualCost の PropertyChanged イベントが発火することを確認する。
    /// </summary>
    [TestMethod]
    public void Tasks_CollectionRemoved_ShouldRaisePropertyChangedForTotalCosts()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task = new ProjectTask();
        task.UpdateEstimatedCost(10);
        task.UpdateActualCost(5);
        var existingTaskViewModel = new ProjectTaskViewModel(task, _userServiceMock.Object);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        project.AddTask(existingTaskViewModel.Model);
        viewModel.SyncFromModel();

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
        project.RemoveTask(existingTaskViewModel.Id);
        viewModel.SyncFromModel();

        // Assert
        Assert.AreEqual(
            1,
            receivedEstimatedCostEvents,
            "タスク削除時にTotalEstimatedCostのPropertyChangedイベントが発火すること"
        );
        Assert.AreEqual(
            1,
            receivedActualCostEvents,
            "タスク削除時にTotalActualCostのPropertyChangedイベントが発火すること"
        );
    }

    /// <summary>
    /// テスト観点: タスクがない場合、合計工数プロパティが 0 となることを確認する。
    /// </summary>
    [TestMethod]
    public void TotalCosts_ShouldBeZero_WhenNoTasks()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

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
        var project = new Project(Guid.Empty);
        var task1 = new ProjectTask();
        task1.UpdateEstimatedCost(10);
        task1.UpdateActualCost(5);
        var task2 = new ProjectTask();
        task2.UpdateEstimatedCost(20);
        task2.UpdateActualCost(10);
        project.AddTask(task1);
        project.AddTask(task2);

        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var taskViewModel1 = viewModel.Tasks.First(t => t.Id == task1.Id);

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
        taskViewModel1.EstimatedCost = 15;
        taskViewModel1.ActualCost = 8;

        // Assert
        // EstimatedCost で 1回、ActualCost で 1回、合計 2回ずつ発火するはず
        Assert.AreEqual(
            2,
            receivedEstimatedCostEvents,
            "タスクの個別のコスト変更によりTotalEstimatedCostのPropertyChangedイベントが発火すること"
        );
        Assert.AreEqual(
            2,
            receivedActualCostEvents,
            "タスクの個別のコスト変更によりTotalActualCostのPropertyChangedイベントが発火すること"
        );
    }

    /// <summary>
    /// テスト観点: タスクを追加した際に、ViewModelのTasksコレクションに重複して追加されないことを確認する。
    /// </summary>
    [TestMethod]
    public void AddTask_ShouldAddOnlyOneViewModel()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Act
        // Model への直接追加後、ViewModel を同期する
        var newTask = new ProjectTask();
        newTask.UpdateName("New Task");
        project.AddTask(newTask);
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
        var project = new Project(Guid.Empty);
        project.UpdateName("Test Project");
        var projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        var task = new ProjectTask();
        task.UpdateName("Existing Task");
        project.AddTask(task);
        projectViewModel.SyncFromModel();

        // この時点で ViewModel.Tasks には1つ入っているはず
        Assert.AreEqual(1, projectViewModel.Tasks.Count, "初期状態でVMのタスクが1つであること");

        // Act
        project.ClearTasks();
        projectViewModel.SyncFromModel();

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
        var baseTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Local);
        var project = new Project(Guid.Empty);
        project.SetUpdatedAt(baseTime.AddSeconds(-secondsOffset));
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

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
        var targetDate = new DateTime(2026, 1, 1, 12, 34, 0, DateTimeKind.Local);
        var project = new Project(Guid.Empty);
        project.SetUpdatedAt(targetDate);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Act
        var actual = viewModel.DisplayLastUpdated;

        // Assert
        var expected = targetDate.ToString("yyyy/MM/dd HH:mm");
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// テスト観点: 更新日時が過去（例：1時間前）であるにもかかわらず、
    /// DateTimeKind の混在やシリアライズの不備により「たった今」と誤表示されないことを確認する。
    /// </summary>
    [TestMethod]
    public void DisplayLastUpdated_ShouldHandlePastTimeCorrectlly_RegardlessOfKind()
    {
        // Arrange
        // 1時間前の時刻を作成 (Local)
        var oneHourAgo = DateTime.Now.AddHours(-1);

        var project = new Project(Guid.Empty);
        project.SetUpdatedAt(oneHourAgo);
        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );

        // Act
        var actual = viewModel.DisplayLastUpdated;

        // Assert
        // 1時間前であれば「1時間前」または「60分前」と表示されるべきであり、「たった今」ではない
        Assert.AreNotEqual("たった今", actual, "過去の日時（1時間前）に対して「たった今」と表示される不具合を再現");
        Assert.IsTrue(actual.Contains("時間前") || actual.Contains("分前"), $"Actual was: {actual}");
    }

    /// <summary>
    /// テスト観点: SyncFromModel を呼び出した際、既に存在するタスクの ViewModel インスタンスが
    /// 維持（再利用）されることを確認する（差分更新の検証）。
    /// </summary>
    [TestMethod]
    public void SyncFromModel_ShouldPreserveExistingViewModelInstances()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var task1 = new ProjectTask();
        task1.UpdateName("Task 1");
        project.AddTask(task1);

        var viewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
        viewModel.SyncFromModel();

        var initialTaskVm1 = viewModel.Tasks.First(t => t.Id == task1.Id);

        // Act
        // 1. タスク2を追加
        var task2 = new ProjectTask();
        task2.UpdateName("Task 2");
        project.AddTask(task2);

        // 2. タスク1の名称をモデル側で書き換え
        task1.UpdateName("Task 1 Updated");

        viewModel.SyncFromModel();

        // Assert
        Assert.AreEqual(2, viewModel.Tasks.Count, "タスクが2つになっていること");

        var currentTaskVm1 = viewModel.Tasks.First(t => t.Id == task1.Id);
        var currentTaskVm2 = viewModel.Tasks.First(t => t.Id == task2.Id);

        Assert.AreSame(initialTaskVm1, currentTaskVm1, "既存タスクの ViewModel インスタンスが再利用されていること");
        Assert.AreEqual(
            "Task 1 Updated",
            currentTaskVm1.Name,
            "再利用された ViewModel のプロパティが更新されていること"
        );
        Assert.AreEqual("Task 2", currentTaskVm2.Name, "新規タスクの ViewModel が正しく作成されていること");
    }
}
