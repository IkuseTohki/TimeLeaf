using System;
using System.Threading.Tasks;
using System.Windows;
using LeafKit.UI.Services;

namespace TimeLeaf.Services;

/// <summary>
/// WPF の Dispatcher を使用して UI スレッドでの実行を行うサービス。
/// </summary>
public class WpfDispatcherService : IDispatcherService
{
    public async Task InvokeAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            await dispatcher.InvokeAsync(action);
        }
        else
        {
            action();
        }
    }

    public async Task InvokeAsync(Func<Task> action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            // InvokeAsync は Task を受け取れないため、内部で await する
            await dispatcher.InvokeAsync(async () => await action());
        }
        else
        {
            await action();
        }
    }
}
