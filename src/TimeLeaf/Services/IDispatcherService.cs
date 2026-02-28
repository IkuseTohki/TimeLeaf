using System;
using System.Threading.Tasks;

namespace TimeLeaf.Services;

/// <summary>
/// UIスレッドでの実行を抽象化するサービスのインターフェース。
/// </summary>
public interface IDispatcherService
{
    /// <summary>
    /// アクションをUIスレッドで非同期に実行します。
    /// </summary>
    Task InvokeAsync(Action action);

    /// <summary>
    /// 非同期関数をUIスレッドで非同期に実行します。
    /// </summary>
    Task InvokeAsync(Func<Task> action);
}
