using System.Threading.Tasks;
using TimeLeaf.Models.Entities;

namespace TimeLeaf.UseCases;

/// <summary>
/// 自分のアイデンティティをプロジェクトの共有ストレージに同期するユースケースのインターフェース。
/// </summary>
public interface ISyncUserIdentityUseCase
{
    /// <summary>
    /// 自分のプロフィール情報を共有ストレージ（users/ フォルダ）に同期します。
    /// </summary>
    /// <returns>非同期タスク。</returns>
    Task ExecuteAsync();
}
