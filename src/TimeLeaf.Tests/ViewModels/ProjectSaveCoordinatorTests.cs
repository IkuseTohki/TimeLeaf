using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class ProjectSaveCoordinatorTests
{
    private Mock<ISaveProjectUseCase> _saveUseCaseMock = null!;
    private Mock<ILogger<ProjectSaveCoordinator>> _loggerMock = null!;
    private Mock<IViewModelFactory> _viewModelFactoryMock = null!;
    private ProjectSaveCoordinator _coordinator = null!;
    private ObservableCollection<ProjectViewModel> _projects = null!;

    [TestInitialize]
    public void Setup()
    {
        _saveUseCaseMock = new Mock<ISaveProjectUseCase>();
        _loggerMock = new Mock<ILogger<ProjectSaveCoordinator>>();
        _viewModelFactoryMock = new Mock<IViewModelFactory>();
        _projects = new ObservableCollection<ProjectViewModel>();
        _coordinator = new ProjectSaveCoordinator(_saveUseCaseMock.Object, _loggerMock.Object);
    }

    [TestMethod]
    public async Task PropertyChanged_ShouldTriggerSave()
    {
        // Arrange
        var project = new Project();
        var vm = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);
        _projects.Add(vm);
        _coordinator.StartMonitoring(_projects);

        // Act
        vm.Name = "Updated Name";
        await Task.Delay(100); // 非同期保存を待機

        // Assert
        _saveUseCaseMock.Verify(x => x.ExecuteAsync(project), Times.Once);
    }

    [TestMethod]
    public async Task CollectionChanged_NewItem_ShouldBeMonitored()
    {
        // Arrange
        _coordinator.StartMonitoring(_projects);
        var project = new Project();
        var vm = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);

        // Act
        _projects.Add(vm);
        vm.Description = "New Desc";
        await Task.Delay(100);

        // Assert
        _saveUseCaseMock.Verify(x => x.ExecuteAsync(project), Times.Once);
    }

    [TestMethod]
    public async Task WhenDisabled_ShouldNotTriggerSave()
    {
        // Arrange
        var project = new Project();
        var vm = new ProjectViewModel(project, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object, _viewModelFactoryMock.Object);
        _projects.Add(vm);
        _coordinator.StartMonitoring(_projects);
        _coordinator.IsEnabled = false;

        // Act
        vm.Name = "Updated Name";
        await Task.Delay(100);

        // Assert
        _saveUseCaseMock.Verify(x => x.ExecuteAsync(It.IsAny<Project>()), Times.Never);
    }
}
