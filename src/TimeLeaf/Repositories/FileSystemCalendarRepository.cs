using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TimeLeaf.Models;
using TimeLeaf.Repositories.Dtos;

namespace TimeLeaf.Repositories
{
    /// <summary>
    /// ファイルシステムを用いたカレンダー設定のリポジトリ実装。
    /// </summary>
    public class FileSystemCalendarRepository : ICalendarRepository
    {
        private readonly string _filePath;
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions { WriteIndented = true };

        public FileSystemCalendarRepository(string filePath)
        {
            _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        public async Task<CalendarSetting> LoadAsync()
        {
            if (!File.Exists(_filePath))
            {
                return new CalendarSetting();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath);
                var dto = JsonSerializer.Deserialize<CalendarSettingDto>(json, _options);

                var setting = new CalendarSetting();
                if (dto != null)
                {
                    foreach (var h in dto.Holidays)
                    {
                        setting.AddHoliday(h.Date, h.Name);
                    }
                    if (dto.WorkdaysOfWeek != null && dto.WorkdaysOfWeek.Any())
                    {
                        setting.SetWorkdays(dto.WorkdaysOfWeek);
                    }
                }
                return setting;
            }
            catch
            {
                return new CalendarSetting();
            }
        }

        public async Task SaveAsync(CalendarSetting setting)
        {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            var dto = new CalendarSettingDto
            {
                Holidays = setting.Holidays.Select(h => new HolidayDto { Date = h, Name = "" }).ToList(),
                WorkdaysOfWeek = setting.WorkdaysOfWeek.ToList(),
            };

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(dto, _options);
            await File.WriteAllTextAsync(_filePath, json);
        }
    }
}
