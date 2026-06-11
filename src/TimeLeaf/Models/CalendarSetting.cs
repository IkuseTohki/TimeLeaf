using System;
using System.Collections.Generic;

namespace TimeLeaf.Models
{
    /// <summary>
    /// カレンダーの稼働日および休日設定を保持するエンティティ。
    /// </summary>
    public class CalendarSetting
    {
        private readonly HashSet<DateTime> _holidays = new HashSet<DateTime>();
        private readonly HashSet<DayOfWeek> _workdaysOfWeek = new HashSet<DayOfWeek>
        {
            DayOfWeek.Monday,
            DayOfWeek.Tuesday,
            DayOfWeek.Wednesday,
            DayOfWeek.Thursday,
            DayOfWeek.Friday,
        };

        /// <summary>
        /// 登録されている休日のリスト。
        /// </summary>
        public IEnumerable<DateTime> Holidays => _holidays;

        /// <summary>
        /// 稼働日として扱う曜日のリスト。
        /// </summary>
        public IEnumerable<DayOfWeek> WorkdaysOfWeek => _workdaysOfWeek;

        /// <summary>
        /// 稼働日として扱う曜日を一括設定します。
        /// </summary>
        public void SetWorkdays(IEnumerable<DayOfWeek> workdays)
        {
            _workdaysOfWeek.Clear();
            foreach (var day in workdays)
            {
                _workdaysOfWeek.Add(day);
            }
        }

        /// <summary>
        /// 特定の日付を休日として登録します。
        /// </summary>
        /// <param name="date">休日とする日付。</param>
        /// <param name="name">休日の名称（任意）。</param>
        public void AddHoliday(DateTime date, string name = "")
        {
            _holidays.Add(date.Date);
        }

        /// <summary>
        /// 指定された曜日が稼働日かどうかを判定します。
        /// </summary>
        public bool IsWorkdayOfWeek(DayOfWeek dayOfWeek)
        {
            return _workdaysOfWeek.Contains(dayOfWeek);
        }

        /// <summary>
        /// 指定された日付が個別の休日として登録されているか判定します。
        /// </summary>
        public bool IsRegisteredHoliday(DateTime date)
        {
            return _holidays.Contains(date.Date);
        }
    }
}
