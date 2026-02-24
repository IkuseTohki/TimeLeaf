using System;

namespace TimeLeaf.Services;

/// <summary>
/// プロジェクトの外部的な変更（同期）を監視するサービスのインターフェース。
/// </summary>
public interface IProjectSyncService
{
    event Action<Guid> ProjectChanged;
    void StartMonitoring();
    void StopMonitoring();
}
