using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace TimeLeaf.Services;

/// <summary>
/// アプリケーションのテーマ切り替えを管理するサービスの実装クラスです。
/// </summary>
public class ThemeService : IThemeService
{
    private readonly IList<ResourceDictionary> _mergedDictionaries;
    private readonly ILogger<ThemeService> _logger;
    private string _currentTheme = "Forest";

    public ThemeService(IList<ResourceDictionary> mergedDictionaries, ILogger<ThemeService> logger)
    {
        _mergedDictionaries = mergedDictionaries ?? throw new ArgumentNullException(nameof(mergedDictionaries));
        _logger = logger;
    }

    /// <inheritdoc/>
    public void ApplyTheme(string themeName)
    {
        _logger.LogInformation("Applying theme: {Theme}", themeName);

        var themeUri = GetThemeUri(themeName);
        if (themeUri == null)
        {
            _logger.LogWarning("Unknown theme requested: {Theme}. Falling back to Forest.", themeName);
            themeName = "Forest";
            themeUri = GetThemeUri(themeName);
        }

        try
        {
            // 既存のテーマリソースを探して差し替える
            var existingTheme = _mergedDictionaries.FirstOrDefault(IsThemeDictionary);

            // Note: ユニットテスト環境では実際の XAML ロードを避けるため、
            // Pack URI ではない Source を持つ ResourceDictionary を作成します。
            var newTheme = CreateResourceDictionary(themeUri!);

            if (existingTheme != null)
            {
                var index = _mergedDictionaries.IndexOf(existingTheme);
                _mergedDictionaries[index] = newTheme;
            }
            else
            {
                // 見つからない場合は先頭に追加（通常はあるはず）
                _mergedDictionaries.Insert(0, newTheme);
            }

            _currentTheme = themeName;
            _logger.LogDebug("Theme {Theme} applied successfully.", themeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {Theme}", themeName);
        }
    }

    /// <summary>
    /// 指定された ResourceDictionary がテーマ（Colors.xaml）を定義するものかどうかを判定します。
    /// </summary>
    protected virtual bool IsThemeDictionary(ResourceDictionary dictionary)
    {
        return dictionary.Source != null && dictionary.Source.OriginalString.Contains("Colors.xaml");
    }

    /// <summary>
    /// ResourceDictionary インスタンスを生成します。
    /// </summary>
    protected virtual ResourceDictionary CreateResourceDictionary(Uri uri)
    {
        return new ResourceDictionary { Source = uri };
    }

    /// <inheritdoc/>
    public string GetCurrentTheme() => _currentTheme;

    private Uri? GetThemeUri(string themeName)
    {
        return themeName.ToLower() switch
        {
            "forest" => new Uri("/Resources/ForestColors.xaml", UriKind.Relative),
            "light" => new Uri("/Resources/LightColors.xaml", UriKind.Relative),
            "dark" => new Uri("/Resources/DarkColors.xaml", UriKind.Relative),
            _ => null,
        };
    }
}
