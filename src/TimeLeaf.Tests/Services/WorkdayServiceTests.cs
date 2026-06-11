using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Services
{
    [TestClass]
    public class WorkdayServiceTests
    {
        private CalendarSetting _setting = null!;
        private WorkdayService _service = null!;

        [TestInitialize]
        public void Initialize()
        {
            _setting = new CalendarSetting();
            _service = new WorkdayService(_setting);
        }

        [TestMethod]
        public void IsWorkday_ShouldReturnFalse_ForWeekendsByDefault()
        {
            // テスト観点: デフォルト設定（土日休み）で週末が非稼働日と判定されること
            var saturday = new DateTime(2026, 6, 6); // Saturday
            var sunday = new DateTime(2026, 6, 7); // Sunday
            var monday = new DateTime(2026, 6, 8); // Monday

            Assert.IsFalse(_service.IsWorkday(saturday), "Saturday should be non-workday");
            Assert.IsFalse(_service.IsWorkday(sunday), "Sunday should be non-workday");
            Assert.IsTrue(_service.IsWorkday(monday), "Monday should be workday");
        }

        [TestMethod]
        public void IsWorkday_ShouldReturnFalse_ForAddedHolidays()
        {
            // テスト観点: 明示的に追加された休日が非稼働日と判定されること
            var holiday = new DateTime(2026, 6, 10);
            _setting.AddHoliday(holiday, "Company Anniversary");

            Assert.IsFalse(_service.IsWorkday(holiday), "Added holiday should be non-workday");
        }

        [TestMethod]
        public void CalculateEndDate_ShouldSkipHolidays()
        {
            // テスト観点: 稼働日数を指定して終了日を計算する際、休日（週末含む）が正しくスキップされること
            // 2026/06/05 (Fri) 開始
            // 稼働日数が 3 日の場合:
            // 1日目: 06/05 (Fri)
            // (06/06 Sat, 06/07 Sun スキップ)
            // 2日目: 06/08 (Mon)
            // 3日目: 06/09 (Tue) -> 終了日

            var start = new DateTime(2026, 6, 5);
            var workdayCount = 3;
            var expectedEnd = new DateTime(2026, 6, 9);

            var actualEnd = _service.CalculateEndDate(start, workdayCount);

            Assert.AreEqual(expectedEnd, actualEnd);
        }
    }
}
