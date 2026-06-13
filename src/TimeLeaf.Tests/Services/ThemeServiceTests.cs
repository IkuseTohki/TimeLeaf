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
    private const string ThemeMarkerKey = "TimeLeafThemeName";

    // 実際の XAML ロードを避けるためのテスト用クラス
    private class TestThemeService : ThemeService
    {
        public TestThemeService(IList<ResourceDictionary> mergedDictionaries, ILogger<ThemeService> logger)
            : base(mergedDictionaries, logger) { }

        protected override ResourceDictionary CreateResourceDictionary(Uri uri)
        {
            // Source をセットせずに、マーカーとパス情報を保持させる
            var dict = new ResourceDictionary();
            dict.Add(ThemeMarkerKey, "Dummy");
            dict.Add("Path", uri.OriginalString);
            return dict;
        }
    }

    [TestInitialize]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ThemeService>>();

        // ダミーのリソースリスト。マーカーを持つ辞書を途中に含める。
        _mergedDictionaries = new List<ResourceDictionary>
        {
            new ResourceDictionary(), // Index 0
            new ResourceDictionary(), // Index 1
            new ResourceDictionary // Index 2 (Theme Slot)
            {
                [ThemeMarkerKey] = "Forest",
            },
            new ResourceDictionary(), // Index 3
        };
    }

    [TestMethod]
    public void ApplyTheme_ShouldReplaceDictionaryWithMarker()
    {
        /*
        テスト観点: 特徴的なキー（マーカー）を持つ辞書が置換されることを確認する。
        */
        var service = new TestThemeService(_mergedDictionaries, _loggerMock.Object);

        service.ApplyTheme("Dark");

        // 要素数は変わらないはず
        Assert.AreEqual(4, _mergedDictionaries.Count);

        // 元々インデックス 2 にあったものが置換されているはず
        Assert.AreEqual("/Resources/Themes/Dark.xaml", _mergedDictionaries[2]["Path"]);
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

        Assert.AreEqual("/Resources/Themes/Forest.xaml", _mergedDictionaries[2]["Path"]);
        Assert.AreEqual("Forest", service.GetCurrentTheme());
    }

    [TestMethod]
    public void ApplyTheme_ShouldAddAtEnd_IfMarkerNotFound()
    {
        /*
        テスト観点: マーカーが見つからない場合、新しく追加されることを確認する。
        */
        _mergedDictionaries.Clear();
        _mergedDictionaries.Add(new ResourceDictionary()); // マーカーなし

        var service = new TestThemeService(_mergedDictionaries, _loggerMock.Object);

        service.ApplyTheme("Light");

        // 1つ追加されて2つになるはず
        Assert.AreEqual(2, _mergedDictionaries.Count);
        Assert.AreEqual("/Resources/Themes/Light.xaml", _mergedDictionaries[1]["Path"]);
    }
}
