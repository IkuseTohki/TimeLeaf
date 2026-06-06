# WPF Resource Validator Tools

WPF リソース（StaticResource/DynamicResource）の整合性を静的に検証し、ランタイムエラーを未然に防ぐためのツール群です。

## ツール一覧

### 1. Invoke-ResourceLinter.ps1 (メインツール)

プロジェクト全域の XAML ファイルを走査し、以下の問題を検出します。

- **欠落リソース (MISSING):** 定義が存在しないキーへの参照。
- **解決順序エラー (ORDER ERR):** 定義は存在するが、WPF のロード順序（MergedDictionaries）の都合で参照元から解決できない StaticResource。

#### 使い方

```powershell
./scripts/ResourceValidator/Invoke-ResourceLinter.ps1
```

### 2. Get-ResourceInventory.ps1 (ユーティリティ)

プロジェクト（TimeLeaf）および共有ライブラリ（LeafKit.UI）内のすべてのリソースをカタログ化します。リンターのエンジンとして機能します。

## 検証対象

- **Markup Extensions:** `{StaticResource Key}`, `{DynamicResource Key}`
- **Attributes:** `BasedOn="{StaticResource Key}"`, `ResourceKey="Key"` 等
- **Tag Extensions:** `<StaticResourceExtension ResourceKey="Key" />`

## 開発フローへの組み込み

新しくスタイルを追加したり、ResourceDictionary を整理した際は、コミット前に必ず `Invoke-ResourceLinter.ps1` を実行して「Integrity OK!」が出ることを確認してください。
これにより、特定の画面を開いた瞬間にアプリがクラッシュする事故を 100% 回避できます。
