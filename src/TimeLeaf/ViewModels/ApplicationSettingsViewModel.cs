using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows;
using TimeLeaf.Models;
using TimeLeaf.Repositories;
using LeafKit.UI.Services;

namespace TimeLeaf.ViewModels
{
    public partial class ApplicationSettingsViewModel : ObservableObject, IDialogViewModel
    {
        private readonly IApplicationSettingsRepository _settingsRepository;
        private readonly ApplicationSettings _currentSettings;

        [ObservableProperty]
        private string _storagePath = string.Empty;

        [ObservableProperty]
        private string _selectedTheme = "Forest";

        [ObservableProperty]
        private bool _enableOsNotification;

        [ObservableProperty]
        private bool _enableAppNotification;

        public event Action<bool>? RequestClose;

        public ApplicationSettingsViewModel(IApplicationSettingsRepository settingsRepository, ApplicationSettings currentSettings)
        {
            _settingsRepository = settingsRepository;
            _currentSettings = currentSettings;

            // 初期値ロード
            StoragePath = _currentSettings.StoragePath;
            SelectedTheme = _currentSettings.Theme;
            EnableOsNotification = _currentSettings.EnableOsNotification;
            EnableAppNotification = _currentSettings.EnableAppNotification;
        }

        [RelayCommand]
        private void Save()
        {
            // 設定オブジェクトを更新
            _currentSettings.Theme = SelectedTheme;
            _currentSettings.EnableOsNotification = EnableOsNotification;
            _currentSettings.EnableAppNotification = EnableAppNotification;
            // StoragePathはここからは変更しない（別コマンドでリセット）

            _settingsRepository.Save(_currentSettings);

            // テーマ適用ロジックが必要だが、今回はリロードを促すか、別途ThemeServiceが必要
            // ここでは簡易的に保存のみ

            RequestClose?.Invoke(true);
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        [RelayCommand]
        private void ResetStoragePath()
        {
            var result = MessageBox.Show(
                "保存場所を変更するにはアプリケーションの再起動が必要です。\n" +
                "設定をリセットして終了しますか？\n" +
                "次回起動時にセットアップ画面が表示されます。",
                "保存場所の変更",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _currentSettings.StoragePath = string.Empty;
                _settingsRepository.Save(_currentSettings);

                // アプリケーション終了
                Application.Current.Shutdown();
            }
        }
    }
}
