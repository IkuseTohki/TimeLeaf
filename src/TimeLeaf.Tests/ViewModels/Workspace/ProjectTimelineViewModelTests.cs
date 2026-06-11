using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models;
using TimeLeaf.Models.Entities;
using TimeLeaf.Models.Enums;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.ViewModels.Workspace;

namespace TimeLeaf.Tests.ViewModels.Workspace
{
    [TestClass]
    public class ProjectTimelineViewModelTests
    {
        private ProjectViewModel _projectViewModel = null!;
        private ProjectTimelineViewModel _viewModel = null!;

        [TestInitialize]
        public void Initialize()
        {
            var project = new Project(Guid.NewGuid());
            project.UpdateName("Test Project");
            project.AddTask(
                new ProjectTask(
                    Guid.NewGuid(),
                    "Task 1",
                    "",
                    TaskStatus.NotStarted,
                    TaskPriority.Medium,
                    null,
                    null,
                    null,
                    null,
                    0,
                    0,
                    null,
                    null,
                    null
                )
            );

            var factoryMock = new Mock<IViewModelFactory>();

            // SyncFromModel 内で呼ばれるファクトリメソッドをセットアップ
            factoryMock
                .Setup(f => f.CreateProjectTaskViewModel(It.IsAny<ProjectTask>()))
                .Returns((ProjectTask t) => new ProjectTaskViewModel(t, new Mock<IUserService>().Object));

            // ProjectViewModel の構築
            _projectViewModel = new ProjectViewModel(
                project,
                Guid.NewGuid(),
                new Mock<IJoinProjectUseCase>().Object,
                factoryMock.Object
            );

            var workdayService = new WorkdayService(new CalendarSetting());
            var useCase = new GetTimelineRowsUseCase(workdayService, new Mock<IUserService>().Object);

            _viewModel = new ProjectTimelineViewModel(_projectViewModel, useCase);
        }

        [TestMethod]
        public void ViewMode_InitialState_IsDualView()
        {
            Assert.AreEqual(TimelineViewMode.Dual, _viewModel.CurrentViewMode);
        }

        [TestMethod]
        public void Rows_ShouldBeLoaded_OnInitialize()
        {
            Assert.IsNotNull(_viewModel.Rows);
            Assert.AreEqual(1, _viewModel.Rows.Count);
            Assert.AreEqual("Task 1", _viewModel.Rows[0].Name);
        }

        [TestMethod]
        public void SetViewMode_ShouldNotifyChange()
        {
            string? changedProperty = null;
            _viewModel.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

            _viewModel.CurrentViewMode = TimelineViewMode.PlanOnly;

            Assert.AreEqual(TimelineViewMode.PlanOnly, _viewModel.CurrentViewMode);
            Assert.AreEqual(nameof(_viewModel.CurrentViewMode), changedProperty);
        }
    }
}
