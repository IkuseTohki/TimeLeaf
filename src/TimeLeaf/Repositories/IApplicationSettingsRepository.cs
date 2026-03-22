using TimeLeaf.Models;

namespace TimeLeaf.Repositories
{
    /// <summary>
    /// アプリケーション設定の読み書きを行うリポジトリインターフェースです。
    /// </summary>
    public interface IApplicationSettingsRepository
    {
        /// <summary>
        /// 現在の設定を読み込みます。設定ファイルが存在しない場合はデフォルト値を返します。
        /// </summary>
        /// <returns>読み込まれたアプリケーション設定。</returns>
        ApplicationSettings Load();

        /// <summary>
        /// 設定を保存します。
        /// </summary>
        /// <param name="settings">保存する設定オブジェクト。</param>
        void Save(ApplicationSettings settings);
    }
}
