# UI リソース管理仕様 (Design System Architecture)

本ドキュメントは、TimeLeaf における WPF リソース（Style, Brush, ControlTemplate 等）の構成、命名規則、および定義原則を規定する。開発者は、新しい UI 要素を追加する際、本仕様に厳格に従わなければならない。

※ 参照: [ADR-0005: UIレイアウトとスタイルの分離方針](../ADR/0005-UIレイアウトとスタイルの分離方針.md)

---

## 1. 設計コンセプトと基本原則

### 1.1 構造と装飾の分離 (Separation of Concerns)

- **View.xaml の責務:** UIの構造 (`Grid`, `StackPanel`)、配置、データバインディングのみを記述する（MUST）。
- **ResourceDictionary の責務:** 装飾やデザイン (`Margin`, `FontSize`, `Foreground` 等) はすべて `<Style>` 定義として抽出する（MUST）。
- **インライン記述の禁止:** コントロール要素に対して `Margin="10"` や `FontSize="14"` などを直接記述してはならない（MUST NOT）。

### 1.2 実体と役割の分離 (Atomic vs Semantic)

本プロジェクトの UI システムは、**「実体 (Atomic / What it is)」** と **「役割 (Semantic / How it is used)」** を完全に分離する 2 層構造を原則とする。

- **実体 (Atomic):** 値そのものを指す不変の定義。具体的なスケール数値や色名で管理される。
- **役割 (Semantic):** UI 上の意図を指す定義。実体を参照（エイリアス）し、将来的な一括変更を可能にする。

---

## 2. ディレクトリ構造とファイル配置

リソースファイルは、その抽象度と役割に応じて以下の 5 つのディレクトリに分類して配置する。

| ディレクトリ    | 役割                                               | 書くべき内容の例                                                     |
| :-------------- | :------------------------------------------------- | :------------------------------------------------------------------- |
| **Tokens/**     | **原子レベル:** 全スタイルの基礎となる定数値。     | 余白の数値（スケール）、フォントサイズ、カラーパレット（実体色）。   |
| **Common/**     | **基盤:** 非視覚的なユーティリティ。               | アイコンの Geometry データ、共通コンバーター、汎用シェアスタイル。   |
| **Controls/**   | **要素レベル:** 標準コントロールの基盤スタイル。   | ボタン、テキスト、入力項目、コンテナ（ListBox/DataGrid等）の基本形。 |
| **Components/** | **分子レベル:** 特定の機能や画面に依存するパーツ。 | サイドバー、ユーザーメニュー、ダイアログ、通知関連等。               |
| **Themes/**     | **テーマ:** 最終的な配色マッピング。               | 役割名（Brush）に対する、テーマごとの色の流し込み。                  |

### 配置ルール

- `Styles.xaml` などの巨大なファイルへの追記は禁止する。
- 汎用的なものは `Controls/` へ、機能固有のものは `Components/` へ適切に切り出すこと。

---

## 3. 命名規則 (Naming Convention)

リソースキーは PascalCase で記述し、以下の構成順序を遵守する。

### 3.1 役割トークン (Styles, Brushes, etc.)

基本構成：**`[対象][場所/機能][役割][状態][種類]`**
インテリセンスでの検索性を優先し、対象(Target)を先頭に置くこと。

- **対象 (Target):** `Button`, `TextBlock`, `Border`, `Grid`, `Run` 等。
- **場所/機能 (Scope):** `Sidebar`, `Dialog`, `TaskCard`, `Dashboard`, `Home` 等。グローバルの場合は省略可。
- **役割 (Role):** `Primary`, `Secondary`, `Header`, `Emphasis`, `Meta`, `Title`, `Link` 等。
- **状態 (State):** `Hover`, `Pressed`, `Disabled`, `Selected` 等。
- **種類 (Type):** `Style`, `Brush`, `Color`, `Geometry`, `Template`, `Converter` を必ず接尾辞として付与する。

**例:**

- `ButtonSidebarPrimaryHoverBrush`
- `TextBlockViewHeaderTitleStyle`
- `BorderDialogRootStyle`
- `RunWorkspaceStatusStyle`

### 3.2 実体トークン (Atomic Scales)

スケールベースの抽象的な ID を使用する。具体的な数値を名前に含めてはならない。

- **余白・角丸:** `Spacing10`, `Spacing20`... / `Radius10`, `Radius20`...
- **フォントサイズ:** `FontScale10`, `FontScale20`...
- **ウェイト:** `Weight100`, `Weight400`, `Weight700`...

---

## 4. テーマと色の管理 (Theming)

### 4.1 テーマ対応の基本方針

- 色や外観はハードコーディングせず、`DynamicResource` を用いてテーマ（Light / Dark / Forest）の動的切り替えを実装する（MUST）。

### 4.2 カラーパレット (`Tokens/Colors.xaml`)

具体的な色名（`Green500`, `Slate100`, `White`, `Black` 等）で Color オブジェクトを定義する。このファイルはテーマに依存せず、すべてのテーマで共有される。

### 4.3 セマンティック・ブラシ (`Themes/*.xaml`)

テーマごとのファイルにおいて、役割名（`BrandPrimaryBrush`, `SurfaceBaseBrush`, `TextPrimaryBrush` 等）に対してパレットの色を割り当てる。

- **原則:** スタイル内ではパレット（`Green500` 等）を直接参照せず、必ずこのセマンティック・ブラシを参照すること。
- **参照:** テーマ切り替えに即時追従するため、ブラシの参照には常に `{DynamicResource}` を使用する（MUST）。

---

## 5. 実装ガイドライン

### 5.1 スタイルの継承 (BasedOn)

記述量の削減と一貫性のため、すべてのスタイルは適切な `BaseStyle` を継承しなければならない（MUST）。

- `Controls/` 内で定義された `BaseStyle` を、`Components/` 内のスタイルが継承する構造を維持すること。

### 5.2 マジックナンバーの禁止

XAML 内でプロパティ（Margin, Padding, FontSize, CornerRadius 等）に数値を直接記述することを禁止する（MUST NOT）。

- 必ず `Tokens/` で定義された役割トークン（例：`{StaticResource StandardPadding}`）を参照すること。

### 5.3 略称の禁止

可読性と検索性を優先し、以下の略称は一切禁止する。

- `Btn` → `Button`
- `Txt` / `Text` (接尾辞として) → `TextBlock`
- `Bg` → `Background`
- `Geom` → `Geometry`
- `Tmpl` → `Template`
- `Conv` → `Converter`
