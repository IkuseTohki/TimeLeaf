using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class AllTasksViewModelTests
{
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private Mock<IUserService> _userServiceMock = null!;

    [TestInitialize]
    public void Setup()
    {
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _userServiceMock = new Mock<IUserService>();

        _viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, _userServiceMock.Object));
    }

    private ProjectViewModel CreateProjectViewModel(Project p)
    {
        return new ProjectViewModel(
            p,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            _viewModelFactoryMock.Object
        );
    }

    /// <summary>
    /// テスト観点: 複数のプロジェクトからタスクが正しく集約されることを確認する。
    /// </summary>
    [TestMethod]
    public void Constructor_ShouldAggregateTasksFromAllProjects()
    {
        // Arrange
        var p1 = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p1.UpdateName("Project 1");
        var t1 = new ProjectTask();
        t1.UpdateName("Task 1-1");
        p1.AddTask(t1);

        var p2 = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p2.UpdateName("Project 2");
        var t2 = new ProjectTask();
        t2.UpdateName("Task 2-1");
        p2.AddTask(t2);

        var projects = new ObservableCollection<ProjectViewModel>
        {
            CreateProjectViewModel(p1),
            CreateProjectViewModel(p2),
        };

        // Act
        var viewModel = new AllTasksViewModel(projects);

        // Assert
        Assert.AreEqual(2, viewModel.AllTasks.Count, "全タスク数が一致すること");
        Assert.IsTrue(viewModel.AllTasks.Any(t => t.Name == "Task 1-1"), "プロジェクト1のタスクが含まれること");
        Assert.IsTrue(viewModel.AllTasks.Any(t => t.Name == "Task 2-1"), "プロジェクト2のタスクが含まれること");
    }

    /// <summary>
    /// テスト観点: プロジェクトが追加された際に、全タスク一覧が自動更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void ProjectsCollectionChanged_ShouldUpdateAllTasks()
    {
        // Arrange
        var projects = new ObservableCollection<ProjectViewModel>();
        var viewModel = new AllTasksViewModel(projects);

        // Act
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p.UpdateName("New Project");
        var t = new ProjectTask();
        t.UpdateName("New Task");
        p.AddTask(t);

        projects.Add(CreateProjectViewModel(p));

        // Assert
        Assert.AreEqual(1, viewModel.AllTasks.Count);
        Assert.AreEqual("New Task", viewModel.AllTasks[0].Name);
    }

    /// <summary>
    /// テスト観点: 検索キーワードによってタスクが正しくフィルタリングされることを確認する。
    /// </summary>
    [TestMethod]
    public void SearchKeyword_ShouldFilterTasks()
    {
        // Arrange
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p.UpdateName("Project");
        var t1 = new ProjectTask();
        t1.UpdateName("Apple");
        p.AddTask(t1);
        var t2 = new ProjectTask();
        t2.UpdateName("Banana");
        p.AddTask(t2);

        var projects = new ObservableCollection<ProjectViewModel> { CreateProjectViewModel(p) };
        var viewModel = new AllTasksViewModel(projects);

        // Act
        viewModel.SearchKeyword = "apple";

        // Assert
        Assert.AreEqual(1, viewModel.AllTasks.Count);
        Assert.AreEqual("Apple", viewModel.AllTasks[0].Name);
    }

    /// <summary>
    /// テスト観点: 未完了のみ表示のフィルタリングが正しく機能することを確認する。
    /// </summary>
    [TestMethod]
    public void ShowOnlyIncomplete_ShouldFilterTasks()
    {
        // Arrange
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p.UpdateName("Project");
        var t1 = new ProjectTask();
        t1.UpdateName("Task 1");
        t1.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.NotStarted);
        p.AddTask(t1);
        var t2 = new ProjectTask();
        t2.UpdateName("Task 2");
        t2.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.Completed);
        p.AddTask(t2);

        var projects = new ObservableCollection<ProjectViewModel> { CreateProjectViewModel(p) };
        var viewModel = new AllTasksViewModel(projects);

        // Act & Assert (初期値は True)
        Assert.IsTrue(viewModel.ShowOnlyIncomplete);
        Assert.AreEqual(1, viewModel.AllTasks.Count, "未完了のみが表示されること");
        Assert.AreEqual("Task 1", viewModel.AllTasks[0].Name);

        // Act 2
        viewModel.ShowOnlyIncomplete = false;

        // Assert 2
        Assert.AreEqual(2, viewModel.AllTasks.Count, "完了したタスクも表示されること");
    }

    /// <summary>
    /// テスト観点: タスクのステータスが「完了」に変更された際、
    /// 「未完了のみ表示」フィルタが有効であればリストから自動的に除外されることを確認する。
    /// </summary>
    [TestMethod]
    public void TaskStatusChanged_ShouldUpdateFiltering()
    {
        // Arrange
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p.UpdateName("Project");
        var t = new ProjectTask();
        t.UpdateName("Task 1");
        t.UpdateStatus(TimeLeaf.Models.Enums.TaskStatus.InProgress);
        p.AddTask(t);

        var projects = new ObservableCollection<ProjectViewModel> { CreateProjectViewModel(p) };
        var viewModel = new AllTasksViewModel(projects);
        Assert.AreEqual(1, viewModel.AllTasks.Count);

        // Act
        // タスクのステータスを「完了」に変更（内部で PropertyChanged が発生する）
        projects[0].Tasks[0].Status = TimeLeaf.Models.Enums.TaskStatus.Completed;

        // Assert
        Assert.AreEqual(0, viewModel.AllTasks.Count, "完了したタスクが自動的にリストから除外されること");
    }

    /// <summary>
    /// テスト観点: タスクの名前が変更された際、検索フィルタの結果が自動的に更新されることを確認する。
    /// </summary>
    [TestMethod]
    public void TaskNameChanged_ShouldUpdateFiltering()
    {
        // Arrange
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        var t = new ProjectTask();
        t.UpdateName("Initial Name");
        p.AddTask(t);

        var projects = new ObservableCollection<ProjectViewModel> { CreateProjectViewModel(p) };
        var viewModel = new AllTasksViewModel(projects);
        viewModel.SearchKeyword = "Updated";
        Assert.AreEqual(0, viewModel.AllTasks.Count);

        // Act
        projects[0].Tasks[0].Name = "Updated Name";

        // Assert
        Assert.AreEqual(1, viewModel.AllTasks.Count, "名前の変更により検索にヒットするようになること");
    }

    /// <summary>
    /// テスト観点: 表示モードを Timeline に切り替えた際に、ICollectionView にグループ化（DeadlineGroup）が適用されることを確認する。
    /// </summary>
    [TestMethod]
    public void CurrentViewMode_SwitchingToTimeline_ShouldApplyGrouping()
    {
        // Arrange
        var projects = new ObservableCollection<ProjectViewModel>();
        var viewModel = new AllTasksViewModel(projects);
        Assert.AreEqual(AllTasksViewMode.List, viewModel.CurrentViewMode);
        Assert.AreEqual(
            0,
            viewModel.AllTasksView.GroupDescriptions.Count,
            "初期状態（List）ではグループ化されていないこと"
        );

        // Act
        viewModel.CurrentViewMode = AllTasksViewMode.Timeline;

        // Assert
        Assert.AreEqual(
            1,
            viewModel.AllTasksView.GroupDescriptions.Count,
            "Timelineモードではグループ化が適用されること"
        );
        var groupDesc = viewModel.AllTasksView.GroupDescriptions[0] as System.Windows.Data.PropertyGroupDescription;
        Assert.IsNotNull(groupDesc);
        Assert.AreEqual(nameof(ProjectTaskViewModel.DeadlineGroup), groupDesc.PropertyName);
    }

    /// <summary>
    /// テスト観点: 表示モードを Timeline から List に戻した際に、グループ化が解除されることを確認する。
    /// </summary>
    [TestMethod]
    public void CurrentViewMode_SwitchingBackToList_ShouldRemoveGrouping()
    {
        // Arrange
        var projects = new ObservableCollection<ProjectViewModel>();
        var viewModel = new AllTasksViewModel(projects);
        viewModel.CurrentViewMode = AllTasksViewMode.Timeline;
        Assert.IsTrue(viewModel.AllTasksView.GroupDescriptions.Count > 0);

        // Act
        viewModel.CurrentViewMode = AllTasksViewMode.List;

        // Assert
        Assert.AreEqual(
            0,
            viewModel.AllTasksView.GroupDescriptions.Count,
            "Listモードに戻すとグループ化が解除されること"
        );
    }

    /// <summary>
    /// テスト観点: ViewModeItems コレクションが正しく初期化され、期待されるすべての表示モードが含まれていることを確認する。
    /// </summary>
    [TestMethod]
    public void ViewModeItems_ShouldContainAllModes()
    {
        // Arrange & Act
        var projects = new ObservableCollection<ProjectViewModel>();
        var viewModel = new AllTasksViewModel(projects);

        // Assert
        Assert.IsNotNull(viewModel.ViewModeItems);
        Assert.AreEqual(3, viewModel.ViewModeItems.Count);
        Assert.IsTrue(viewModel.ViewModeItems.Any(i => i.Value == AllTasksViewMode.List));
        Assert.IsTrue(viewModel.ViewModeItems.Any(i => i.Value == AllTasksViewMode.Timeline));
        Assert.IsTrue(viewModel.ViewModeItems.Any(i => i.Value == AllTasksViewMode.Grid));
    }

    /// <summary>
    /// テスト観点: ProjectTaskViewModel の GlobalRequestDetail イベントが発生した際に、
    /// AllTasksViewModel がそれを検知して RequestNavigation イベントを発生させることを確認する。
    /// </summary>
    [TestMethod]
    public void GlobalRequestDetail_ShouldTriggerRequestNavigation()
    {
        // Arrange
        var p = new Project(Guid.Empty) { Id = Guid.NewGuid() };
        p.UpdateName("Target Project");
        var t = new ProjectTask();
        t.UpdateName("Target Task");
        p.AddTask(t);

        var pvm = CreateProjectViewModel(p);
        var projects = new ObservableCollection<ProjectViewModel> { pvm };
        var viewModel = new AllTasksViewModel(projects);

        var taskVm = pvm.Tasks[0];

        ProjectViewModel? navigatedProject = null;
        ProjectTaskViewModel? navigatedTask = null;
        viewModel.RequestNavigation += (s, e) =>
        {
            navigatedProject = e.Project;
            navigatedTask = e.Task;
        };

        // Act
        taskVm.RequestDetailCommand.Execute(null);

        // Assert
        Assert.IsNotNull(navigatedProject, "RequestNavigation イベントが発生すること");
        Assert.AreEqual(pvm, navigatedProject, "正しいプロジェクトが渡されること");
        Assert.AreEqual(taskVm, navigatedTask, "正しいタスクが渡されること");
    }
}
