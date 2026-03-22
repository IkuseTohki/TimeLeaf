using System.Threading.Tasks;
using TimeLeaf.Repositories.FileSystem.Dtos;

namespace TimeLeaf.Repositories;

/// <summary>
/// アイデンティティシードファイル（seed.json）へのアクセスを抽象化するリポジトリインターフェース。
/// </summary>
public interface IIdentitySeedRepository
{
    /// <summary>
    /// シードデータを読み込みます。存在しない場合はnullを返します。
    /// </summary>
    Task<IdentitySeedDto?> LoadAsync();

    /// <summary>
    /// シードデータを保存します。
    /// </summary>
    Task SaveAsync(IdentitySeedDto seed);

    /// <summary>
    /// 現在のシードファイルのパスを取得します。
    /// </summary>
    string GetFilePath();
}
