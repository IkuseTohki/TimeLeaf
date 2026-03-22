using System;

namespace TimeLeaf.Models
{
    /// <summary>
    /// アプリケーション全体のローカル設定を管理するモデルクラスです。
    /// ユーザーごとの設定（AppDataなど）として保存されます。
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// データ保存場所のルートパスを取得または設定します。
        /// </summary>
        public string StoragePath { get; set; } = string.Empty;

        /// <summary>
        /// アプリケーションのテーマを取得または設定します。
        /// </summary>
        public string Theme { get; set; } = "Forest";

        /// <summary>
        /// OS標準の通知を利用するかどうかを取得または設定します。
        /// </summary>
        public bool EnableOsNotification { get; set; } = true;

        /// <summary>
        /// アプリ内通知（スナックバー等）を利用するかどうかを取得または設定します。
        /// </summary>
        public bool EnableAppNotification { get; set; } = true;
    }
}
