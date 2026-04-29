using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels.Workspace;

[TestClass]
public class ProjectFlowViewModelTests
{
    [TestMethod]
    public void Constructor_ShouldPopulateTaskNodes()
    {
        // Arrange
        var project = new Project();
        project.AddTask(new ProjectTask { Id = Guid.NewGuid() });
        project.AddTask(new ProjectTask { Id = Guid.NewGuid() });

        var layoutUseCase = new CalculateFlowLayoutUseCase();
        var userServiceMock = new Mock<TimeLeaf.Services.IUserService>();
        var viewModelFactoryMock = new Mock<IViewModelFactory>();

        viewModelFactoryMock
            .Setup(x => x.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
            .Returns((ProjectTask t) => new ProjectTaskViewModel(t, userServiceMock.Object));

        var projectViewModel = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            viewModelFactoryMock.Object
        );
        // Act
        var vm = new ProjectFlowViewModel(projectViewModel, layoutUseCase, userServiceMock.Object);

        // Assert
        Assert.AreEqual(2, vm.TaskNodes.Count);
    }
}
