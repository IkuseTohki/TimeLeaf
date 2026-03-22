using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.Services;
using LeafKit.UI.Services;

namespace TimeLeaf.Tests.ViewModels;

[TestClass]
public class HomeViewModelTests
{
    private UserMenuViewModel CreateUserMenu()
    {
        var mockIdentity = new Mock<IIdentityService>();
        var mockDialog = new Mock<IDialogService>();
        return new UserMenuViewModel(mockIdentity.Object, mockDialog.Object);
    }

    [TestMethod]
    public void TaskStats_ShouldReflectAllProjects()
    {
        // Arrange
        var p1 = new Project();
        p1.UpdateName("P1");
        var t1 = new ProjectTask();
        t1.UpdateStatus(TaskStatus.InProgress);
        p1.AddTask(t1);

        var p2 = new Project();
        p2.UpdateName("P2");
        var t2 = new ProjectTask();
        t2.UpdateStatus(TaskStatus.NotStarted);
        p2.AddTask(t2);

        var projects = new ObservableCollection<ProjectViewModel>
        {
            new ProjectViewModel(p1, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object),
            new ProjectViewModel(p2, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object)
        };

        // Act
        var viewModel = new HomeViewModel(projects, CreateUserMenu());

        // Assert
        Assert.AreEqual(1, viewModel.NotStartedCount);
        Assert.AreEqual(1, viewModel.InProgressCount);
    }

    [TestMethod]
    public void UpcomingDeadlines_ShouldIncludeTasksFromAllProjects()
    {
        // Arrange
        var p1 = new Project();
        var t1 = new ProjectTask();
        t1.UpdateName("Task 1");
        t1.UpdateSchedule(null, DateTime.UtcNow.AddDays(1));
        p1.AddTask(t1);

        var p2 = new Project();
        var t2 = new ProjectTask();
        t2.UpdateName("Task 2");
        t2.UpdateSchedule(null, DateTime.UtcNow.AddDays(2));
        p2.AddTask(t2);

        var projects = new ObservableCollection<ProjectViewModel>
        {
            new ProjectViewModel(p1, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object),
            new ProjectViewModel(p2, Guid.NewGuid(), new Mock<IJoinProjectUseCase>().Object)
        };

        // Act
        var viewModel = new HomeViewModel(projects, CreateUserMenu());

        // Assert
        Assert.AreEqual(2, viewModel.UpcomingDeadlines.Count);
        Assert.AreEqual("Task 1", viewModel.UpcomingDeadlines[0].Name);
    }
}
