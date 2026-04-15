using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;

namespace TimeLeaf.Services.Tests;

[TestClass]
public class ProjectServiceTests
{
    private Mock<IProjectRepository> _repositoryMock = null!;
    private Mock<IIdentityService> _identityServiceMock = null!;
    private Mock<ILogger<ProjectService>> _loggerMock = null!;
    private ProjectService _projectService = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repositoryMock = new Mock<IProjectRepository>();
        _identityServiceMock = new Mock<IIdentityService>();
        _loggerMock = new Mock<ILogger<ProjectService>>();

        _projectService = new ProjectService(_repositoryMock.Object, _identityServiceMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task LoadAllAsync_ClearsCacheAndLoadsProjects()
    {
        // Arrange
        /* テスト観点: 全プロジェクトのロードを呼び出した際、キャッシュがリセットされ、リポジトリからのデータが格納されることを確認する。 */
        var p1 = new Project() { Id = Guid.NewGuid() };
        p1.UpdateName("P1");
        var p2 = new Project() { Id = Guid.NewGuid() };
        p2.UpdateName("P2");
        var projects = new List<Project> { p1, p2 };
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(projects);

        // Act
        await _projectService.LoadAllAsync();

        // Assert
        Assert.AreEqual(2, _projectService.AllProjects.Count());
        _repositoryMock.Verify(r => r.LoadAllAsync(), Times.Once);
    }

    [TestMethod]
    public async Task GetProjectAsync_ReturnsFromCacheIfExists()
    {
        // Arrange
        /* テスト観点: プロジェクトがキャッシュに存在する場合、リポジトリにアクセスせずにキャッシュから取得することを確認する。 */
        var projectId = Guid.NewGuid();
        var project = new Project() { Id = projectId };
        project.UpdateName("Test");
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { project });
        await _projectService.LoadAllAsync(); // Cache it

        // Act
        var result = await _projectService.GetProjectAsync(projectId);

        // Assert
        Assert.AreEqual(project, result);
        _repositoryMock.Verify(r => r.LoadAsync(It.IsAny<Guid>()), Times.Never);
    }

    [TestMethod]
    public async Task SaveProjectAsync_FiresProjectAdded_WhenNewProject()
    {
        // Arrange
        /* テスト観点: 新規プロジェクトを保存した際、ProjectAddedイベントが発火されることを確認する。 */
        var project = new Project() { Id = Guid.NewGuid() };
        project.UpdateName("New Project");
        var userId = Guid.NewGuid();
        _identityServiceMock.Setup(i => i.CurrentUserId).Returns(userId);

        Project? eventArgs = null;
        _projectService.ProjectAdded += (p) => eventArgs = p;

        // Act
        await _projectService.SaveProjectAsync(project);

        // Assert
        Assert.AreEqual(project, eventArgs);
        _repositoryMock.Verify(r => r.SaveAsync(project, userId.ToString()), Times.Once);
    }

    [TestMethod]
    public async Task SaveProjectAsync_FiresProjectUpdated_WhenExistingProject()
    {
        // Arrange
        /* テスト観点: 既存プロジェクトを保存した際、ProjectUpdatedイベントが発火されることを確認する。 */
        var projectId = Guid.NewGuid();
        var project = new Project() { Id = projectId };
        project.UpdateName("Existing Project");
        _repositoryMock.Setup(r => r.LoadAllAsync()).ReturnsAsync(new List<Project> { project });
        await _projectService.LoadAllAsync(); // Add to cache

        var userId = Guid.NewGuid();
        _identityServiceMock.Setup(i => i.CurrentUserId).Returns(userId);

        Project? eventArgs = null;
        _projectService.ProjectUpdated += (p) => eventArgs = p;

        // Act
        await _projectService.SaveProjectAsync(project);

        // Assert
        Assert.AreEqual(project, eventArgs);
        _repositoryMock.Verify(r => r.SaveAsync(project, userId.ToString()), Times.Once);
    }

    [TestMethod]
    public async Task OnProjectExternalChanged_SyncsCacheAndFiresUpdated()
    {
        // Arrange
        /* テスト観点: リポジトリのProjectChangedイベントを受けた際、キャッシュが最新化されProjectUpdatedが発火することを確認する。 */
        var projectId = Guid.NewGuid();
        var updatedProject = new Project() { Id = projectId };
        updatedProject.UpdateName("Updated Externally");
        _repositoryMock.Setup(r => r.LoadAsync(projectId)).ReturnsAsync(updatedProject);

        Project? eventArgs = null;
        _projectService.ProjectUpdated += (p) => eventArgs = p;

        // Act
        // OnProjectExternalChanged は private なので、リポジトリのイベントを発火させる
        _repositoryMock.Raise(r => r.ProjectChanged += null, projectId);

        // async void なので少し待つ
        await Task.Delay(50);

        // Assert
        Assert.AreEqual(updatedProject, eventArgs);
        _repositoryMock.Verify(r => r.LoadAsync(projectId), Times.AtLeastOnce);
    }
}
