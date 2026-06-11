using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeLeaf.Models;
using TimeLeaf.Repositories;

namespace TimeLeaf.Tests.Infrastructure
{
    [TestClass]
    public class FileSystemCalendarRepositoryTests
    {
        private string _testDir = null!;
        private string _filePath = null!;
        private FileSystemCalendarRepository _repository = null!;

        [TestInitialize]
        public void Initialize()
        {
            _testDir = Path.Combine(Path.GetTempPath(), "TimeLeafTests_Calendar", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            _filePath = Path.Combine(_testDir, "calendar.json");
            _repository = new FileSystemCalendarRepository(_filePath);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, true);
            }
        }

        [TestMethod]
        public void LoadAsync_ShouldReturnDefault_WhenFileDoesNotExist()
        {
            // テスト観点: ファイルが存在しない場合、デフォルト設定（土日休み）が返されること
            var setting = _repository.LoadAsync().Result;

            Assert.IsNotNull(setting);
            Assert.AreEqual(5, setting.WorkdaysOfWeek.Count());
            Assert.IsTrue(setting.IsWorkdayOfWeek(DayOfWeek.Monday));
            Assert.IsFalse(setting.IsWorkdayOfWeek(DayOfWeek.Sunday));
            Assert.AreEqual(0, setting.Holidays.Count());
        }

        [TestMethod]
        public async Task SaveAndLoad_ShouldPreserveSettings()
        {
            // テスト観点: 保存した設定が正しく読み込めること
            var setting = new CalendarSetting();
            var holiday = new DateTime(2026, 6, 10);
            setting.AddHoliday(holiday, "Anniversary");

            // 週末を稼働日に変えてみる
            // (CalendarSettingに曜日変更メソッドを追加する必要があるかもしれません)

            await _repository.SaveAsync(setting);

            var loaded = await _repository.LoadAsync();

            Assert.IsTrue(loaded.IsRegisteredHoliday(holiday));
            Assert.AreEqual(1, loaded.Holidays.Count());
        }
    }
}
