namespace TimeLeaf.Services
{
    /// <summary>
    /// アプリケーションのテーマ切り替えを管理するサービスインターフェースです。
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// 指定されたテーマをアプリケーションに適用します。
        /// </summary>
        /// <param name="themeName">適用するテーマ名（Forest, Light, Darkなど）。</param>
        void ApplyTheme(string themeName);

        /// <summary>
        /// 現在適用されているテーマ名を取得します。
        /// </summary>
        /// <returns>テーマ名。</returns>
        string GetCurrentTheme();
    }
}
