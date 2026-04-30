using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TimeLeaf.Services;

namespace TimeLeaf.Tests.Services;

[TestClass]
public class ThemeServiceTests
{
    private Mock<ILogger<ThemeService>> _loggerMock = null!;
    private List<ResourceDictionary> _mergedDictionaries = null!;

    // 実際の XAML ロードを避けるためのテスト用クラス
    private class TestThemeService : ThemeService
    {
        public const string ThemeKey = "IsTheme";

        public TestThemeService(IList<ResourceDictionary> mergedDictionaries, ILogger<ThemeService> logger)
            : base(mergedDictionaries, logger) { }

        protected override bool IsThemeDictionary(ResourceDictionary dictionary)
        {
            // Source の代わりにダミーキーで判定
            return dictionary.Contains(ThemeKey);
        }

        protected override ResourceDictionary CreateResourceDictionary(Uri uri)
        {
            // Source をセットせずに、検証用の情報を保持させる
            var dict = new ResourceDictionary();
            dict.Add(ThemeKey, true);
            dict.Add("Path", uri.OriginalString);
            return dict;
        }
    }

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ThemeService>>();
        // ResourceDictionary 自体も Source をセットするとロードを試みるため、
        // 初期状態の辞書も Source なしで作成する
        var forestTheme = new ResourceDictionary();
        forestTheme.Add(TestThemeService.ThemeKey, true);
        forestTheme.Add("Path", "ForestColors.xaml");

        var icons = new ResourceDictionary();
        icons.Add("Path", "Icons.xaml");

        _mergedDictionaries = new List<ResourceDictionary> { forestTheme, icons };
    }

    [TestMethod]
    public void ApplyTheme_ShouldReplaceExistingTheme()
    {
        /*
        テスト観点: 既存のテーマ辞書が、指定した新しいテーマの辞書に置換されることを確認する。
        */
        var service = new TestThemeService(_mergedDictionaries, _loggerMock.Object);

        service.ApplyTheme("Dark");

        Assert.AreEqual(2, _mergedDictionaries.Count);
        var theme = _mergedDictionaries.FirstOrDefault(d => d.Contains(TestThemeService.ThemeKey));
        Assert.IsNotNull(theme);
        Assert.AreEqual("/Resources/DarkColors.xaml", theme["Path"]);
        Assert.AreEqual("Dark", service.GetCurrentTheme());
    }

    [TestMethod]
    public void ApplyTheme_ShouldFallbackToForest_WhenUnknownThemeRequested()
    {
        /*
        テスト観点: 未知のテーマ名が渡された場合、デフォルトのForestテーマが適用されることを確認する。
        */
        var service = new TestThemeService(_mergedDictionaries, _loggerMock.Object);

        service.ApplyTheme("InvalidTheme");

        var theme = _mergedDictionaries.FirstOrDefault(d => d.Contains(TestThemeService.ThemeKey));
        Assert.IsNotNull(theme);
        Assert.AreEqual("/Resources/ForestColors.xaml", theme["Path"]);
        Assert.AreEqual("Forest", service.GetCurrentTheme());
    }

    [TestMethod]
    public void ApplyTheme_ShouldWorkEvenIfNoThemeExists()
    {
        /*
        テスト観点: 初期状態でテーマ辞書が存在しない場合でも、新しく追加されることを確認する。
        */
        _mergedDictionaries.Clear();
        var service = new TestThemeService(_mergedDictionaries, _loggerMock.Object);

        service.ApplyTheme("Light");

        Assert.AreEqual(1, _mergedDictionaries.Count);
        Assert.AreEqual("/Resources/LightColors.xaml", _mergedDictionaries[0]["Path"]);
    }
}
