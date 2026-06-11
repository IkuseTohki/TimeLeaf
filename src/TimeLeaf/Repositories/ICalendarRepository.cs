using System.Threading.Tasks;
using TimeLeaf.Models;

namespace TimeLeaf.Repositories
{
    /// <summary>
    /// カレンダー設定の永続化を担うリポジトリ。
    /// </summary>
    public interface ICalendarRepository
    {
        /// <summary>
        /// カレンダー設定を読み込みます。
        /// </summary>
        Task<CalendarSetting> LoadAsync();

        /// <summary>
        /// カレンダー設定を保存します。
        /// </summary>
        Task SaveAsync(CalendarSetting setting);
    }
}
