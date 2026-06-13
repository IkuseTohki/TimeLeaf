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
    private const string ThemeMarkerKey = "TimeLeafThemeName";

    public ThemeService(IList<ResourceDictionary> mergedDictionaries, ILogger<ThemeService> logger)
    {
        _mergedDictionaries = mergedDictionaries ?? throw new ArgumentNullException(nameof(mergedDictionaries));
        _logger = logger;
    }

    /// <inheritdoc/>
    public void ApplyTheme(string themeName)
    {
        _logger.LogInformation("Attempting to apply theme: {Theme}", themeName);

        var themeUri = GetThemeUri(themeName);
        if (themeUri == null)
        {
            _logger.LogWarning("Unknown theme requested: {Theme}. Falling back to Forest.", themeName);
            themeName = "Forest";
            themeUri = GetThemeUri(themeName);
        }

        try
        {
            var existingTheme = _mergedDictionaries.FirstOrDefault(d => d.Contains(ThemeMarkerKey));
            var newTheme = CreateResourceDictionary(themeUri!);

            if (existingTheme != null)
            {
                var index = _mergedDictionaries.IndexOf(existingTheme);
                _mergedDictionaries[index] = newTheme;
            }
            else
            {
                var iconsEntry = _mergedDictionaries.FirstOrDefault(d =>
                    d.Source != null && d.Source.OriginalString.Contains("Icons.xaml")
                );
                if (iconsEntry != null)
                {
                    var index = _mergedDictionaries.IndexOf(iconsEntry);
                    _mergedDictionaries.Insert(index, newTheme);
                }
                else
                {
                    _mergedDictionaries.Add(newTheme);
                }
            }

            _currentTheme = themeName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply theme: {Theme}", themeName);
        }
    }

    protected virtual ResourceDictionary CreateResourceDictionary(Uri uri)
    {
        return new ResourceDictionary { Source = uri };
    }

    public string GetCurrentTheme() => _currentTheme;

    private Uri? GetThemeUri(string themeName)
    {
        return themeName.ToLower() switch
        {
            "forest" => new Uri("/Resources/Themes/Forest.xaml", UriKind.Relative),
            "light" => new Uri("/Resources/Themes/Light.xaml", UriKind.Relative),
            "dark" => new Uri("/Resources/Themes/Dark.xaml", UriKind.Relative),
            _ => null,
        };
    }
}
