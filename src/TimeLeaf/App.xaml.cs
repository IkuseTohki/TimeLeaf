using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TimeLeaf.Models.Interfaces;
using TimeLeaf.Repositories.Json;
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
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "projects.json");
        services.AddSingleton<IProjectRepository>(new JsonProjectRepository(filePath));

        // ユースケースの登録
        services.AddTransient<LoadProjectsUseCase>();
        services.AddTransient<SaveProjectsUseCase>();

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

