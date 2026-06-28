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

namespace TimeLeaf.Tests.UseCases
{
    [TestClass]
    public class GetTimelineRowsUseCaseTests
    {
        private GetTimelineRowsUseCase _useCase = null!;
        private Mock<IUserService> _userServiceMock = null!;

        [TestInitialize]
        public void Initialize()
        {
            var setting = new CalendarSetting();
            var workdayService = new WorkdayService(setting);
            _userServiceMock = new Mock<IUserService>();

            _useCase = new GetTimelineRowsUseCase(workdayService, _userServiceMock.Object);
        }

        [TestMethod]
        public void Execute_ShouldReturnRowsInHierarchicalOrder()
        {
            // Arrange
            // テスト観点: タスクが親子階層順（親 -> その子 -> 次の親）に並ぶこと
            var project = new Project(Guid.NewGuid());
            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var otherId = Guid.NewGuid();

            var parent = new ProjectTask { Id = parentId };
            parent.UpdateName("Parent");
            var child = new ProjectTask { Id = childId };
            child.UpdateName("Child");
            var other = new ProjectTask { Id = otherId };
            other.UpdateName("Other");

            project.AddTask(parent);
            project.AddTask(child);
            project.AddTask(other);

            var container = new ProjectContainer(Guid.NewGuid(), "Container") { ProjectId = project.Id };
            project.AddContainer(container);
            container.AddChild(child);

            // ルート順序の設定 [Parent, Container, Other]
            project.ReorderWorkItems(new[] { parent.Id, container.Id, other.Id });

            var rootContainer = new ProjectRootContainer(project);

            // Act
            var rows = _useCase.Execute(rootContainer).ToList();

            // Assert
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual("Parent", rows[0].Name);
            Assert.AreEqual("Container", rows[1].Name);
            Assert.AreEqual("Child", rows[2].Name); // Container の子
            Assert.AreEqual("Other", rows[3].Name);

            Assert.AreEqual(0, rows[0].Depth);
            Assert.AreEqual(0, rows[1].Depth);
            Assert.AreEqual(1, rows[2].Depth);
            Assert.AreEqual(0, rows[3].Depth);
        }

        [TestMethod]
        public void Execute_ShouldCalculateProgressOffset_Correctly()
        {
            // Arrange
            // テスト観点: 稲妻線の偏差計算が正しいこと
            var today = new DateTime(2026, 6, 6);
            var project = new Project(Guid.NewGuid());
            var task = new ProjectTask();
            task.UpdateName("Task");
            task.UpdateSchedule(new DateTime(2026, 6, 1), new DateTime(2026, 6, 10));
            task.UpdateEstimatedCost(10.0);
            task.UpdateActualCost(8.0); // 80% 進捗 (50% 経過時点)

            project.AddTask(task);
            var rootContainer = new ProjectRootContainer(project);

            // Act
            var rows = _useCase.Execute(rootContainer, today).ToList();
            var row = rows.First();

            // Assert
            Assert.IsTrue(row.ProgressOffsetDays > 0, $"Expected positive offset, but got {row.ProgressOffsetDays}");
        }

        [TestMethod]
        public void Execute_ShouldSetUserInformation_WhenAssigneeExists()
        {
            // Arrange
            // テスト観点: 担当者が設定されている場合、ユーザーサービスから名前を取得しイニシャルを設定すること
            var project = new Project(Guid.NewGuid());
            var userId = Guid.NewGuid();
            var task = new ProjectTask();
            task.UpdateName("Task");
            task.AssignTo(userId);

            project.AddTask(task);
            var rootContainer = new ProjectRootContainer(project);

            _userServiceMock.Setup(s => s.GetUserName(userId.ToString())).Returns("Sato");

            // Act
            var rows = _useCase.Execute(rootContainer).ToList();
            var row = rows.First();

            // Assert
            Assert.AreEqual("Sato", row.DisplayName);
            Assert.AreEqual("#0984e3", row.ThemeColor);
        }
    }
}
