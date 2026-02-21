using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.Views;

namespace TimeLeaf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // 外部依存の設定
        // 仕様に基づき、プロジェクトごとのフォルダを管理するルートディレクトリを指定
        var storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "storage");

        services.AddSingleton<ICurrentUserService, WindowsCurrentUserService>();
        services.AddSingleton<IProjectRepository>(sp =>
            new FolderProjectRepository(storagePath, sp.GetRequiredService<ICurrentUserService>()));

        // ユースケースの登録
        services.AddTransient<LoadProjectsUseCase>();
        services.AddTransient<SaveProjectsUseCase>();
        services.AddTransient<SaveProjectUseCase>();

        // ViewModel の登録
        services.AddTransient<MainViewModel>();

        // View の登録
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // DIコンテナからメインウィンドウを取得して表示
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.Show();
    }
}

