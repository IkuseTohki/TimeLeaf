using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LeafKit.UI.Services;
using TimeLeaf.Repositories;

namespace TimeLeaf.ViewModels
{
    /// <summary>
    /// アプリケーション初回起動時のセットアップ処理を担当する ViewModel です。
    /// データ保存先の指定と初期化を行います。
    /// </summary>
    public partial class SetupViewModel : ObservableObject, IDialogViewModel
    {
        private readonly IApplicationSettingsRepository _settingsRepository;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAndStartCommand))]
        private string _storagePath = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        /// <inheritdoc/>
        public event Action<bool>? RequestClose;

        /// <summary>
        /// セットアップ用の ViewModel を初期化します。
        /// </summary>
        /// <param name="settingsRepository">設定保存用リポジトリ。</param>
        /// <param name="dialogService">ダイアログ表示用サービス。</param>
        public SetupViewModel(IApplicationSettingsRepository settingsRepository, IDialogService dialogService)
        {
            _settingsRepository = settingsRepository;
            _dialogService = dialogService;

            var current = _settingsRepository.Load();
            StoragePath = current.StoragePath;
        }

        /// <summary>
        /// フォルダ参照ダイアログを表示し、保存場所を選択します。
        /// </summary>
        [RelayCommand]
        private void BrowseStoragePath()
        {
            var path = _dialogService.ShowFolderBrowserDialog("プロジェクト管理フォルダを選択してください");
            if (!string.IsNullOrWhiteSpace(path))
            {
                StoragePath = path;
            }
        }

        private bool CanSaveAndStart => !string.IsNullOrWhiteSpace(StoragePath) && Directory.Exists(StoragePath);

        /// <summary>
        /// 設定を保存し、アプリケーションを開始します。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveAndStart))]
        private void SaveAndStart()
        {
            try
            {
                if (!Directory.Exists(StoragePath))
                {
                    ErrorMessage = "指定されたフォルダが存在しません。";
                    return;
                }

                var settings = _settingsRepository.Load();
                settings.StoragePath = StoragePath;
                _settingsRepository.Save(settings);

                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"設定の保存に失敗しました: {ex.Message}";
            }
        }

        /// <summary>
        /// セットアップをキャンセルします。
        /// </summary>
        [RelayCommand]
        private void Cancel() => RequestClose?.Invoke(false);
    }
}
