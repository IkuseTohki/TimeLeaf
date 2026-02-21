using System.IO;
using System.Windows;
using TimeLeaf.Repositories.Json;
using TimeLeaf.ViewModels;
using TimeLeaf.Views;

namespace TimeLeaf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 保存先パスの設定（実行ファイルと同じ階層の projects.json）
        var filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "projects.json");

        // リポジトリの初期化
        var repository = new JsonProjectRepository(filePath);

        // ViewModel の初期化（依存注入）
        var mainViewModel = new MainViewModel(repository);

        // メインウィンドウの生成と表示
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };
        mainWindow.Show();
    }
}

