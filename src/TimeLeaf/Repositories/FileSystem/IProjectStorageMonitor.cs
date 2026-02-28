using System;

namespace TimeLeaf.Repositories.FileSystem;

/// <summary>
/// プロジェクトストレージ内のファイル変更を監視するコンポーネントのインターフェース。
/// </summary>
public interface IProjectStorageMonitor : IDisposable
{
    /// <summary>
    /// プロジェクトのデータに変更（外部からの書き込み等）があった際に発生するイベント。
    /// </summary>
    event Action<Guid> ProjectChanged;

    /// <summary>
    /// 指定されたファイル名を「自前で書き込んだファイル」として登録し、
    /// 直後の変更イベントを無視するようにします。
    /// </summary>
    /// <param name="fileName">ファイル名。</param>
    void MarkFileAsJustWritten(string fileName);
}
