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
        var p1 = new Project();
        p1.UpdateName("Project 1");
        var p2 = new Project();
        p2.UpdateName("Project 2");
        var originalProjects = new List<Project> { p1, p2 };

        // Act
        await repository.SaveAllAsync(originalProjects, "test-user");
        var loadedProjects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(2, loadedProjects.Count);
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
        var project = new Project();
        project.UpdateName("Project with Tasks");
        project.AddTask(new ProjectTask { Name = "Task 1" });
        project.AddTask(new ProjectTask { Name = "Task 2" });

        var originalProjects = new List<Project> { project };

        // Act
        await repository.SaveAllAsync(originalProjects, "test-user");
        var loadedProjects = (await repository.LoadAllAsync()).ToList();

        // Assert
        Assert.AreEqual(1, loadedProjects.Count);
        Assert.AreEqual(2, loadedProjects[0].Tasks.Count);
        Assert.AreEqual("Task 1", loadedProjects[0].Tasks[0].Name);
        Assert.AreEqual("Task 2", loadedProjects[0].Tasks[1].Name);
    }
}
