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
using LeafKit.Services;
using LeafKit.System.Services;
using LeafKit.UI.Services;

namespace TimeLeaf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;
    private static bool _isErrorDialogShowing = false;

    public App()
    {
        // 未ハンドルの例外をキャッチする
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        // Serilog の設定
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/timeleaf-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }
        catch (Exception ex)
        {
            HandleGlobalException(ex, "App Constructor Exception");
        }
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        HandleGlobalException(e.Exception, "UI Thread Dispatcher Exception");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        HandleGlobalException(e.ExceptionObject as Exception, "AppDomain Unhandled Exception");
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs e)
    {
        HandleGlobalException(e.Exception, "TaskScheduler Unobserved Exception");
        e.SetObserved();
    }

    private void HandleGlobalException(Exception? ex, string type)
    {
        if (ex == null) return;

        // すでにエラーダイアログが表示されている場合は何もしない（ログのみ）
        lock (typeof(App))
        {
            if (_isErrorDialogShowing)
            {
                Log.Warning("Additional error suppressed while error dialog is showing: {Message}", ex.Message);
                return;
            }
            _isErrorDialogShowing = true;
        }

        // ログに記録して即座にフラッシュする
        Log.Fatal(ex, "Critical Unhandled Error [{Type}]: {Message}", type, ex.Message);
        Log.CloseAndFlush();

        var detail = $"【エラーの種類】: {type}\n" +
                     $"【メッセージ】: {ex.Message}\n\n" +
                     $"【スタックトレース】:\n{ex}";

        void ShowErrorWindow()
        {
            try
            {
                var errorWin = new FatalErrorWindow(detail);
                errorWin.ShowDialog();
            }
            catch (Exception fallbackEx)
            {
                // XAMLパースエラーなどで FatalErrorWindow 自体が表示できない場合の最終手段
                MessageBox.Show(
                    $"致命的なエラーが発生しました。さらに、エラーダイアログの表示にも失敗しました。\n\n" +
                    $"元のエラー: {ex.Message}\n\n" +
                    $"表示エラー: {fallbackEx.Message}",
                    "TimeLeaf 致命的なエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                // ダイアログを閉じた後は、さらなる例外発生を防ぐためにアプリケーションを即座に終了させる。
                // 致命的なエラーであるため、状態の不整合を防ぐためにもプロセスレベルでの終了が望ましい。
                Environment.Exit(1);
            }
        }

        // UIスレッドでウィンドウを表示する
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(ShowErrorWindow);
        }
        else
        {
            ShowErrorWindow();
        }
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
        var usersPath = Path.Combine(storagePath, "users");

        services.AddSingleton<IIdentitySeedRepository>(sp =>
        {
            var portableDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            var homeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".timeleaf");
            return new FileIdentitySeedRepository(portableDir, homeDir);
        });
        services.AddSingleton<IIdentityService, FileBasedIdentityService>();
        services.AddSingleton<IUserRepository>(sp => new FileSystemUserRepository(usersPath));

        services.AddSingleton<ICurrentUserService, WindowsCurrentUserService>();
        services.AddSingleton<IDispatcherService, WpfDispatcherService>();
        services.AddSingleton<ISingleInstanceService, SingleInstanceService>();

        // 永続化層のコンポーネント登録
        services.AddSingleton<IProjectFileSystemSerializer, JsonProjectFileSystemSerializer>();
        services.AddSingleton<ICommitFileNameGenerator, DefaultCommitFileNameGenerator>();
        services.AddSingleton<IProjectStorageMonitor>(sp =>
            new FileSystemProjectStorageMonitor(storagePath, sp.GetRequiredService<ILogger<FileSystemProjectStorageMonitor>>()));

        services.AddSingleton<IProjectRepository>(sp =>
            new FolderProjectRepository(
                storagePath,
                sp.GetRequiredService<IProjectStorageMonitor>(),
                sp.GetRequiredService<IProjectFileSystemSerializer>(),
                sp.GetRequiredService<ICommitFileNameGenerator>(),
                sp.GetRequiredService<ILogger<FolderProjectRepository>>()));

        // LeafKit.UI サービスの登録
        services.AddSingleton<IDialogService, DialogService>();

        // アプリケーションサービスの登録
        services.AddSingleton<IProjectSyncService, ProjectSyncService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<IOSNotificationService, WindowsNotificationService>();
        services.AddSingleton<IViewModelFactory, ViewModelFactory>();

        // ユースケースの登録
        services.AddTransient<ILoadProjectsUseCase, LoadProjectsUseCase>();
        services.AddTransient<ISaveProjectUseCase, SaveProjectUseCase>();
        services.AddTransient<IFindProjectUseCase, FindProjectUseCase>();
        services.AddTransient<IAddProjectUseCase, AddProjectUseCase>();
        services.AddTransient<IAddTaskUseCase, AddTaskUseCase>();
        services.AddTransient<IAddCommentUseCase, AddCommentUseCase>();
        services.AddTransient<IAddMilestoneUseCase, AddMilestoneUseCase>();
        services.AddTransient<ICheckTaskDeadlinesUseCase, CheckTaskDeadlinesUseCase>();
        services.AddTransient<ISyncUserIdentityUseCase, SyncUserIdentityUseCase>();
        services.AddTransient<IJoinProjectUseCase, JoinProjectUseCase>();
        services.AddTransient<ICheckAssignmentUseCase, CheckAssignmentUseCase>();

        // コーディネーターの登録
        services.AddSingleton<IProjectSaveCoordinator, ProjectSaveCoordinator>();

        // ViewModel の登録
        services.AddTransient<MainViewModel>();
        services.AddTransient<AddProjectViewModel>();
        services.AddTransient<AddTaskViewModel>();
        // Note: OverviewViewModel と ProjectWorkspaceViewModel はファクトリ経由で生成されるため、直接の Transient 登録は不要だが、
        // ファクトリ内での GetRequiredService 用に登録しておく。
        services.AddTransient<OverviewViewModel>();
        services.AddTransient<ProjectWorkspaceViewModel>();

        // View の登録
        services.AddTransient<MainWindow>();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            Log.Information("Application Starting Up");

            var singleInstanceService = _serviceProvider.GetRequiredService<ISingleInstanceService>();
            const string AppId = "TimeLeaf-App-Instance";

            if (!singleInstanceService.Start(AppId))
            {
                Log.Information("Another instance is already running. Notifying and exiting.");
                singleInstanceService.NotifyFirstInstance(AppId);
                this.Shutdown();
                return;
            }

            // 自分のアイデンティティを同期 (UI 表示前に先行して同期を完了させる)
            var syncUseCase = _serviceProvider.GetRequiredService<ISyncUserIdentityUseCase>();
            await syncUseCase.ExecuteAsync();

            // DIコンテナからメインウィンドウを取得
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

            // 多重起動通知を受けた際のアクティブ化設定
            singleInstanceService.LaunchedAnotherInstance += (s, ev) =>
            {
                mainViewModel.IsWindowVisible = true;
            };

            mainWindow.DataContext = mainViewModel;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            HandleGlobalException(ex, "Application Start-up Exception");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("Application Shutting Down");
        _serviceProvider?.GetService<ISingleInstanceService>()?.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

