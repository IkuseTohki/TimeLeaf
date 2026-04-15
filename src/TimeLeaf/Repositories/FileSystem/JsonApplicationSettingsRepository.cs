using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TimeLeaf.Models;
using TimeLeaf.Repositories;

namespace TimeLeaf.Repositories.FileSystem
{
    /// <summary>
    /// ローカルファイルシステム上のJSONファイルでアプリケーション設定を管理するリポジトリです。
    /// 保存先: %USERPROFILE%/.timeleaf/settings.json
    /// </summary>
    public class JsonApplicationSettingsRepository : IApplicationSettingsRepository
    {
        private readonly string _settingsFilePath;
        private readonly JsonSerializerOptions _jsonOptions;

        public JsonApplicationSettingsRepository()
        {
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDir = Path.Combine(userProfilePath, ".timeleaf");

            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }

            _settingsFilePath = Path.Combine(appDir, "settings.json");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };
        }

        /// <inheritdoc/>
        public ApplicationSettings Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new ApplicationSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<ApplicationSettings>(json, _jsonOptions);
                return settings ?? new ApplicationSettings();
            }
            catch (Exception)
            {
                // ファイル破損等の場合はデフォルト設定を返す（またはログ出力して再生成）
                return new ApplicationSettings();
            }
        }

        /// <inheritdoc/>
        public void Save(ApplicationSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // ここでの例外は上位に伝播させるか、ログ出力が必要
                throw new IOException($"設定ファイルの保存に失敗しました: {_settingsFilePath}", ex);
            }
        }
    }
}
