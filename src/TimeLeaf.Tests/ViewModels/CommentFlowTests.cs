using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Repositories;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class CommentFlowTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<ICurrentUserService> _userServiceMock = null!;
    private Mock<IServiceProvider> _serviceProviderMock = null!;
    private Mock<IDialogService> _dialogServiceMock = null!;
    private MainViewModel _mainViewModel = null!;

    [TestInitialize]
    public async Task Setup()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _userServiceMock = new Mock<ICurrentUserService>();
        _userServiceMock.Setup(u => u.GetCurrentUserId()).Returns("test-user");

        var loadUseCaseMock = new Mock<ILoadProjectsUseCase>();
        var findProjectUseCaseMock = new Mock<IFindProjectUseCase>();
        var syncServiceMock = new Mock<IProjectSyncService>();
        var addProjectUseCaseMock = new Mock<IAddProjectUseCase>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var loggerMock = new Mock<ILogger<MainViewModel>>();
        _dialogServiceMock = new Mock<IDialogService>();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<ProjectWorkspaceViewModel>)))
            .Returns(new Mock<ILogger<ProjectWorkspaceViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ILogger<OverviewViewModel>)))
            .Returns(new Mock<ILogger<OverviewViewModel>>().Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(ICurrentUserService)))
            .Returns(_userServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IAddProjectUseCase)))
            .Returns(addProjectUseCaseMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDialogService)))
            .Returns(_dialogServiceMock.Object);
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceProvider)))
            .Returns(_serviceProviderMock.Object);

        var project = new Project();
        project.UpdateName("Test Project");
        var task = new ProjectTask();
        task.UpdateName("Test Task");
        project.AddTask(task);

        loadUseCaseMock.Setup(r => r.ExecuteAsync()).ReturnsAsync(new[] { project });

        // Factory mock setup
        viewModelFactoryMock.Setup(x => x.CreateOverviewViewModel(It.IsAny<ObservableCollection<ProjectViewModel>>()))
            .Returns((ObservableCollection<ProjectViewModel> p) => new OverviewViewModel(p, addProjectUseCaseMock.Object, _dialogServiceMock.Object, _serviceProviderMock.Object, new Mock<ILogger<OverviewViewModel>>().Object));

        viewModelFactoryMock.Setup(x => x.CreateProjectWorkspaceViewModel(It.IsAny<ProjectViewModel>()))
            .Returns((ProjectViewModel pvm) => new ProjectWorkspaceViewModel(pvm, _userServiceMock.Object, new Mock<ILogger<ProjectWorkspaceViewModel>>().Object));

        _mainViewModel = new MainViewModel(
            loadUseCaseMock.Object,
            _saveUseCaseMock.Object,
            findProjectUseCaseMock.Object,
            syncServiceMock.Object,
            addProjectUseCaseMock.Object,
            viewModelFactoryMock.Object,
            loggerMock.Object);

        await Task.Delay(500); // 確実に初期化！EnitializeAsync�E�を征E��
    }

    /// <summary>
    /// テスト観点: WorkspaceViewModel でコメントを追加した際、
    /// リポジトリの SaveAsync が呼び出されることを確認する。
    /// </summary>
    [TestMethod]
    public async Task AddComment_ShouldTriggerRepositorySave()
    {
        // Arrange
        await Task.Delay(500);
        var projectViewModel = _mainViewModel.Projects.FirstOrDefault();
        Assert.IsNotNull(projectViewModel, "初期ロードでプロジェクトが取得できていること");

        _mainViewModel.NavigateToProjectCommand.Execute(projectViewModel);
        var workspaceViewModel = (ProjectWorkspaceViewModel)_mainViewModel.CurrentViewModel;

        workspaceViewModel.SelectedTask = workspaceViewModel.Tasks.First();
        workspaceViewModel.NewCommentContent = "New test comment";

        // Act
        workspaceViewModel.AddCommentCommand.Execute(null);
        await Task.Delay(200); // 自動保存の完了を待つ

        // Assert
        _saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p =>
            p.Tasks.Any(t => t.Comments.Any(c => c.Content == "New test comment")))),
            Times.AtLeastOnce, "コメント追加により保存が走ること");

        Assert.AreEqual(string.Empty, workspaceViewModel.NewCommentContent);
    }

    /// <summary>
    /// テスト観点: タスクのリストがリセット（再ロード等）されても、
    /// 同じIDのタスクが選択状態として復元されることを確認する。
    /// </summary>
    [TestMethod]
    public void TaskSelection_ShouldBePreserved_AfterCollectionReset()
    {
        // Arrange
        var projectViewModel = _mainViewModel.Projects.First();
        var userServiceMock = new Mock<ICurrentUserService>();
        var loggerMock = new Mock<ILogger<ProjectWorkspaceViewModel>>();
        var workspaceViewModel = new ProjectWorkspaceViewModel(projectViewModel, userServiceMock.Object, loggerMock.Object);

        var targetTask = workspaceViewModel.Tasks.First();
        workspaceViewModel.SelectedTask = targetTask;
        var taskId = targetTask.Id;

        // Act
        // モデルのコレクションをクリアして再追加（再ロードのシミュレーション）
        var taskEntity = projectViewModel.Model.Tasks.First();
        projectViewModel.Model.ClearTasks();
        projectViewModel.Model.AddTask(taskEntity);
        projectViewModel.SyncFromModel(); // 手動同期

        // Assert
        Assert.IsNotNull(workspaceViewModel.SelectedTask, "再ロード後に選択状態が復元されていること");
        Assert.AreEqual(taskId, workspaceViewModel.SelectedTask.Id);
    }

    /// <summary>
    /// テスト観点: 外部からの同期(ProjectChanged)によってViewModelが作り直された後も、
    /// そのタスクに対する変更が正しく保存をトリガーすることを確認する。
    /// </summary>
    [TestMethod]
    public async Task AutoSave_ShouldWork_AfterProjectSync()
    {
        // Arrange
        var projectViewModel = _mainViewModel.Projects.First();
        var projectId = projectViewModel.Id;

        // 外部同期イベントを発生させて、ViewModel内部の状態を更新させる
        var updatedProject = new Project { Id = projectId };
        updatedProject.UpdateName("Synced Project");
        var updatedTask = new ProjectTask(
            projectViewModel.Tasks.First().Id,
            "Synced Task",
            "",
            TimeLeaf.Models.Enums.TaskStatus.NotStarted,
            TaskPriority.Medium,
            null, null, null, null, 0, 0, "", null, null
        );
        updatedProject.AddTask(updatedTask);
        _repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(updatedProject);

        _repositoryMock.Raise(r => r.ProjectChanged += null, projectId);
        await Task.Delay(200); // 同期処理の完了を待つ

        // Act
        // 同期後に新しくリストに入ったタスクViewModelを取得して変更を加える
        var reloadedTaskVM = _mainViewModel.Projects.First().Tasks.First();
        reloadedTaskVM.Assignee = "New Author After Sync";

        await Task.Delay(200); // 自動保存の完了を待つ

        // Assert
        _saveUseCaseMock.Verify(r => r.ExecuteAsync(It.Is<Project>(p =>
            p.Tasks.Any(t => t.Assignee == "New Author After Sync"))),
            Times.AtLeastOnce, "同期後のタスクに対する変更も保存が実行されること");
    }
}
