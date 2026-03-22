using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using TimeLeaf.Repositories;

namespace TimeLeaf.ViewModels
{
    public partial class SetupViewModel : ObservableObject
    {
        private readonly IApplicationSettingsRepository _settingsRepository;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveAndStartCommand))]
        private string _storagePath = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public event Action? RequestClose;

        public SetupViewModel(IApplicationSettingsRepository settingsRepository)
        {
            _settingsRepository = settingsRepository;

            // 既存の設定があれば読み込む（再設定の場合などを考慮）
            var current = _settingsRepository.Load();
            StoragePath = current.StoragePath;
        }

        private bool CanSaveAndStart => !string.IsNullOrWhiteSpace(StoragePath) && Directory.Exists(StoragePath);

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

                // 設定を保存
                var settings = _settingsRepository.Load();
                settings.StoragePath = StoragePath;
                _settingsRepository.Save(settings);

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"設定の保存に失敗しました: {ex.Message}";
            }
        }

        public void SetPath(string path)
        {
            StoragePath = path;
        }
    }
}
