using System;
using TimeLeaf.Models;

namespace TimeLeaf.Services
{
    /// <summary>
    /// カレンダー設定に基づき、稼働日の判定や期間計算を行うドメインサービス。
    /// </summary>
    public class WorkdayService
    {
        private readonly CalendarSetting _setting;

        public WorkdayService(CalendarSetting setting)
        {
            _setting = setting ?? throw new ArgumentNullException(nameof(setting));
        }

        /// <summary>
        /// 指定された日付が稼働日かどうかを判定します。
        /// </summary>
        public bool IsWorkday(DateTime date)
        {
            if (!_setting.IsWorkdayOfWeek(date.DayOfWeek))
                return false;
            if (_setting.IsRegisteredHoliday(date))
                return false;
            return true;
        }

        /// <summary>
        /// 開始日と稼働日数を指定して、終了日を計算します。
        /// </summary>
        /// <param name="start">開始日。</param>
        /// <param name="workdayCount">稼働日数（工数から算出される期間）。</param>
        /// <returns>終了日。</returns>
        public DateTime CalculateEndDate(DateTime start, int workdayCount)
        {
            if (workdayCount <= 0)
                return start;

            var current = start.Date;
            var countedDays = 0;

            while (true)
            {
                if (IsWorkday(current))
                {
                    countedDays++;
                }

                if (countedDays >= workdayCount)
                {
                    return current;
                }

                current = current.AddDays(1);
            }
        }
    }
}
