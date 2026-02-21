using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories.Json;

namespace TimeLeaf.Tests.Repositories;

[TestClass]
public class JsonProjectRepositoryTests
{
    private string _tempFilePath = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempFilePath = Path.GetTempFileName();
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    /// <summary>
    /// テスト観点: プロジェクトリストをJSONとして保存し、正しく読み戻せることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_ShouldPreserveData()
    {
        // Arrange
        var repository = new JsonProjectRepository(_tempFilePath);
        var originalProjects = new List<Project>
        {
            new Project { Name = "Project 1" },
            new Project { Name = "Project 2" }
        };

        // Act
        await repository.SaveAllAsync(originalProjects);
        var loadedProjects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.HasCount(2, loadedProjects);
        Assert.AreEqual("Project 1", loadedProjects[0].Name);
        Assert.AreEqual("Project 2", loadedProjects[1].Name);
    }

    /// <summary>
    /// テスト観点: タスクが含まれるプロジェクトを保存し、タスクも正しく読み戻せることを確認する。
    /// </summary>
    [TestMethod]
    public async System.Threading.Tasks.Task SaveAndLoad_WithTasks_ShouldPreserveTasks()
    {
        // Arrange
        var repository = new JsonProjectRepository(_tempFilePath);
        var project = new Project { Name = "Project with Tasks" };
        project.Tasks.Add(new Models.Entities.Task { Name = "Task 1" });
        project.Tasks.Add(new Models.Entities.Task { Name = "Task 2" });

        var originalProjects = new List<Project> { project };

        // Act
        await repository.SaveAllAsync(originalProjects);
        var loadedProjects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.HasCount(1, loadedProjects);
        Assert.HasCount(2, loadedProjects[0].Tasks);
        Assert.AreEqual("Task 1", loadedProjects[0].Tasks[0].Name);
        Assert.AreEqual("Task 2", loadedProjects[0].Tasks[1].Name);
    }
}
