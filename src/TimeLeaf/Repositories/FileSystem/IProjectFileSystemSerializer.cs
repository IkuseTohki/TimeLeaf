namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// プロジェクトに関連するデータをファイルシステム向けにシリアライズ・デシリアライズするためのインターフェース。
/// </summary>
public interface IProjectFileSystemSerializer
{
    /// <summary>
    /// オブジェクトをシリアライズして文字列（JSON）に変換します。
    /// </summary>
    string Serialize<T>(T dto)
        where T : class;

    /// <summary>
    /// 文字列（JSON）をオブジェクトにデシリアライズします。
    /// </summary>
    T? Deserialize<T>(string data)
        where T : class;
}
