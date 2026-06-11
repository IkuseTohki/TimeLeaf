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
            var projectId = Guid.NewGuid();
            var parentId = Guid.NewGuid();
            var childId = Guid.NewGuid();
            var otherId = Guid.NewGuid();

            var parent = new ProjectTask(
                parentId,
                "Parent",
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
            );
            var child = new ProjectTask(
                childId,
                "Child",
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
            );
            child.SetParentId(parentId);
            var other = new ProjectTask(
                otherId,
                "Other",
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
            );

            var tasks = new List<ProjectTask> { other, parent, child };

            // Act
            var rows = _useCase.Execute(tasks).ToList();

            // Assert
            var parentIdx = rows.FindIndex(r => r.TaskId == parentId);
            var childIdx = rows.FindIndex(r => r.TaskId == childId);

            Assert.IsTrue(parentIdx < childIdx, "Parent should come before Child");
            Assert.AreEqual(0, rows[parentIdx].Depth);
            Assert.AreEqual(1, rows[childIdx].Depth);
        }

        [TestMethod]
        public void Execute_ShouldCalculateProgressOffset_Correctly()
        {
            // Arrange
            // テスト観点: 稲妻線の偏差計算が正しいこと
            // シナリオ:
            // 予定期間: 6/1 ~ 6/10 (10日間)
            // 今日: 6/6 (50%経過時点)
            // 進捗: 工数ベース 80% (実績8 / 見積10) -> 先行

            var today = new DateTime(2026, 6, 6);
            var taskId = Guid.NewGuid();

            var task = new ProjectTask(
                taskId,
                "Task",
                "",
                TaskStatus.InProgress,
                TaskPriority.Medium,
                new DateTime(2026, 6, 1),
                new DateTime(2026, 6, 10),
                null,
                null,
                10.0,
                8.0,
                null,
                null,
                null
            );

            // Act
            var rows = _useCase.Execute(new[] { task }, today).ToList();
            var row = rows.First();

            // Assert
            Assert.IsTrue(row.ProgressOffsetDays > 0, $"Expected positive offset, but got {row.ProgressOffsetDays}");
        }

        [TestMethod]
        public void Execute_ShouldSetUserInformation_WhenAssigneeExists()
        {
            // Arrange
            // テスト観点: 担当者が設定されている場合、ユーザーサービスから名前を取得しイニシャルを設定すること
            var userId = Guid.NewGuid();
            var task = new ProjectTask(
                Guid.NewGuid(),
                "Task",
                "",
                TaskStatus.NotStarted,
                TaskPriority.Medium,
                null,
                null,
                null,
                null,
                0,
                0,
                userId,
                null,
                null
            );

            _userServiceMock.Setup(s => s.GetUserName(userId.ToString())).Returns("Sato");

            // Act
            var rows = _useCase.Execute(new[] { task }).ToList();
            var row = rows.First();

            // Assert
            Assert.AreEqual("S", row.UserInitial);
            Assert.AreEqual("#0984e3", row.UserColor);
        }
    }
}
