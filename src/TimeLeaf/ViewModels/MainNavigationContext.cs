namespace TimeLeaf.ViewModels;

/// <summary>
/// アプリケーション全体のナビゲーションコンテキスト（表示モード）を定義します。
/// </summary>
public enum MainNavigationContext
{
    /// <summary>
    /// 未設定
    /// </summary>
    None,

    /// <summary>
    /// ホーム（グローバルダッシュボード）
    /// </summary>
    Home,

    /// <summary>
    /// すべてのタスク（プロジェクト横断一覧）
    /// </summary>
    AllTasks,

    /// <summary>
    /// 通知センター
    /// </summary>
    Notifications,

    /// <summary>
    /// 個別プロジェクトの詳細（ワークスペース）
    /// </summary>
    ProjectDetail
}
