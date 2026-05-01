using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Models.Entities;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;

namespace TimeLeaf.Tests.Infrastructure;

[TestClass]
public class DateTimeHandlingTests
{
    private readonly ICommitFileNameGenerator _generator = new DefaultCommitFileNameGenerator();

    [TestMethod]
    public void CommitFileName_ShouldPreserveTimePoint_RegardlessOfLocalTime()
    {
        /*
         * テスト観点:
         * 日本標準時(JST)などのローカル時刻でファイルを生成し、それをパースした際に、
         * 9時間のズレ（JSTの場合）が発生しないことを確認する。
         */

        // 敢えてローカル時刻を作成 (Kind = Local)
        // 2026/02/26 23:40:00
        var localTime = new DateTime(2026, 2, 26, 23, 40, 0, DateTimeKind.Local);

        // ファイル名を生成
        var userId = "user1";
        var category = "ProjectBasic";
        var fileName = _generator.Generate(localTime, userId, category);

        // パース
        var parsed = _generator.Parse(fileName);

        // 検証: パースされた時刻がそのままLocal時刻として一致すべき
        Assert.AreEqual(DateTimeKind.Local, parsed.Timestamp.Kind, "パースされた時刻はLocalであるべき");
        Assert.AreEqual(localTime, parsed.Timestamp, "パースされた時刻が元の入力と一致すべき");
    }

    [TestMethod]
    [DataRow("2024-02-29T12:00:00.000Z", "閏年のテスト")]
    [DataRow("2026-12-31T23:59:59.999Z", "年末ギリギリのテスト")]
    [DataRow("2026-01-01T00:00:00.000Z", "年始のテスト")]
    public void CommitFileName_EdgeCases_ShouldParseCorrectly(string isoDate, string comment)
    {
        // Arrange
        var timestamp = DateTime.Parse(isoDate, null, DateTimeStyles.RoundtripKind);
        var userId = "user1";
        var category = "Category";

        // Act
        var fileName = _generator.Generate(timestamp, userId, category);
        var parsed = _generator.Parse(fileName);

        // Assert
        Assert.AreEqual(timestamp, parsed.Timestamp, comment);
    }

    [TestMethod]
    public void CommitFileName_Sorting_ShouldBeCorrectAcrossBoundary()
    {
        // Arrange: 年をまたぐ境界値
        var t1 = new DateTime(2025, 12, 31, 23, 59, 59, 999, DateTimeKind.Utc);
        var t2 = new DateTime(2026, 01, 01, 00, 00, 00, 001, DateTimeKind.Utc);

        var f1 = _generator.Generate(t1, "u", "C");
        var f2 = _generator.Generate(t2, "u", "C");

        // Act
        var sorted = new[] { f2, f1 }.OrderBy(x => x).ToList();

        // Assert
        Assert.AreEqual(f1, sorted[0], "年末のファイルが先に来るべき");
        Assert.AreEqual(f2, sorted[1], "年始のファイルが後に来るべき");
    }

    [TestMethod]
    public void DisplayLastUpdated_ShouldHandleRelativeTimesCorrectly()
    {
        // Arrange
        var project = new Project(Guid.Empty);
        var viewModelFactoryMock = new Mock<IViewModelFactory>();
        var vm = new ProjectViewModel(
            project,
            Guid.NewGuid(),
            new Mock<IJoinProjectUseCase>().Object,
            viewModelFactoryMock.Object
        );
        var nowLocal = DateTime.Now;

        // Act & Assert

        // 1. たった今 (Local)
        vm.UpdatedAt = nowLocal.AddSeconds(-5);
        Assert.AreEqual("たった今", vm.DisplayLastUpdated);

        // 2. 5分前
        vm.UpdatedAt = nowLocal.AddMinutes(-5);
        Assert.AreEqual("5分前", vm.DisplayLastUpdated);

        // 3. 2時間前
        vm.UpdatedAt = nowLocal.AddHours(-2);
        Assert.AreEqual("2時間前", vm.DisplayLastUpdated);

        // 4. 昨日 (24時間以上前)
        var yesterday = nowLocal.AddHours(-25);
        vm.UpdatedAt = yesterday;
        Assert.AreEqual(yesterday.ToString("yyyy/MM/dd HH:mm"), vm.DisplayLastUpdated);

        // 5. 未来の日時 (同期ズレなどで発生しうる)
        var future = nowLocal.AddMinutes(1);
        vm.UpdatedAt = future;
        Assert.AreEqual(future.ToString("yyyy/MM/dd HH:mm"), vm.DisplayLastUpdated);
    }

    [TestMethod]
    public void ProjectTaskViewModel_ShouldHandleLocalDatesFromUI()
    {
        // Arrange
        var task = new ProjectTask();
        var userServiceMock = new Mock<IUserService>();
        var vm = new ProjectTaskViewModel(task, userServiceMock.Object);
        var localDate = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Local);

        // Act
        vm.ScheduledStartDate = localDate;

        // Assert
        // ドメインモデルに渡された値が正しいか確認
        Assert.IsNotNull(task.ScheduledStartDate);
        Assert.AreEqual(localDate, task.ScheduledStartDate.Value);
        // 内部的にLocalとして保持されている
        Assert.AreEqual(
            DateTimeKind.Local,
            task.ScheduledStartDate.Value.Kind,
            "ドメイン層ではLocalとして保持されるべき"
        );
    }

    [TestMethod]
    public void ProjectMetadata_Serialization_ShouldBeIso8601WithMilliseconds()
    {
        // Arrange
        var timestamp = new DateTime(2026, 2, 26, 15, 30, 45, 789, DateTimeKind.Utc);
        var dto = new
        {
            ProjectId = Guid.NewGuid(),
            CreatedAt = timestamp,
            CreatedBy = "test",
            SchemaVersion = 1,
        };
        var options = new System.Text.Json.JsonSerializerOptions();

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(dto, options);

        // Assert
        // .NET 7+ のデフォルトシリアライザは ISO 8601 形式で、ミリ秒を含み、Zがつくはず
        // 例: "2026-02-26T15:30:45.789Z"
        StringAssert.Contains(json, "2026-02-26T15:30:45.789Z");
    }

    [TestMethod]
    public void DateTime_Deserialization_ShouldBeRobust()
    {
        // Arrange: タイムゾーン指定なし(Local扱い)のJSON
        var json = "{\"Time\": \"2026-02-26T15:30:45.000\"}";

        // Act
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var time = doc.RootElement.GetProperty("Time").GetDateTime();

        // Assert
        // System.Text.Json の GetDateTime は Kind=Unspecified でパースする（タイムゾーン指定がない場合）
        // これを絶対時間として扱うには注意が必要。
        // TimeLeaf では「ファイル名」を真実の時刻とするが、.project の CreatedAt はメタデータとして重要。
        Assert.AreEqual(2026, time.Year);
        Assert.AreEqual(15, time.Hour);
    }
}
