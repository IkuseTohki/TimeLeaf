using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TimeLeaf.Repositories;
using TimeLeaf.Repositories.FileSystem;
using TimeLeaf.Services;
using TimeLeaf.UseCases;
using TimeLeaf.ViewModels;
using TimeLeaf.Views;
using LeafKit.UI.Services;

namespace TimeLeaf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    public App()
    {
        // Serilog の設定
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/timeleaf-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // ログの設定
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(dispose: true);
        });

        // 外部依存の設定
        // 仕様に基づき、プロジェクトごとのフォルダを管理するルートディレクトリを指定
        var storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "storage");

        services.AddSingleton<ICurrentUserService, WindowsCurrentUserService>();
        services.AddSingleton<IProjectRepository>(sp =>
            new FolderProjectRepository(storagePath, sp.GetRequiredService<ILogger<FolderProjectRepository>>()));

        // LeafKit.UI サービスの登録
        services.AddSingleton<IDialogService, DialogService>();

        // アプリケーションサービスの登録
        services.AddSingleton<IProjectSyncService, ProjectSyncService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();

        // ユースケースの登録
        services.AddTransient<ILoadProjectsUseCase, LoadProjectsUseCase>();
        services.AddTransient<ISaveProjectUseCase, SaveProjectUseCase>();
        services.AddTransient<IFindProjectUseCase, FindProjectUseCase>();
        services.AddTransient<IAddProjectUseCase, AddProjectUseCase>();
        services.AddTransient<IAddTaskUseCase, AddTaskUseCase>();
        services.AddTransient<IAddCommentUseCase, AddCommentUseCase>();
        services.AddTransient<IAddMilestoneUseCase, AddMilestoneUseCase>();

        // ViewModel の登録
        services.AddTransient<MainViewModel>();
        services.AddTransient<AddProjectViewModel>();
        // Note: OverviewViewModel と ProjectWorkspaceViewModel はファクトリ経由で生成されるため、直接の Transient 登録は不要だが、
        // ファクトリ内での GetRequiredService 用に登録しておく。
        services.AddTransient<OverviewViewModel>();
        services.AddTransient<ProjectWorkspaceViewModel>();

        // View の登録
        services.AddTransient<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Log.Information("Application Starting Up");
            // DIコンテナからメインウィンドウを取得して表示
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application start-up failed");
            throw;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application Shutting Down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

