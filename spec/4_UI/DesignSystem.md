# UIレイアウトとスタイルの分離方針 (Design System)

※ 参照: ADR-0005

## 1. 構造と装飾の分離 (Separation of Concerns)

- **View.xaml の責務:** UIの構造 (`Grid`, `StackPanel`)、配置、データバインディングのみを記述する（MUST）。
- **ResourceDictionary の責務:** 装飾やデザイン (`Margin`, `FontSize`, `Foreground` 等) はすべて `<Style>` 定義として抽出する（MUST）。
- **インライン記述の禁止:** コントロール要素に対して `Margin="10"` や `FontSize="14"` などを直接記述してはならない（MUST NOT）。

## 2. テーマ対応 (Theming)

- 色や外観はハードコーディングせず、`DynamicResource` を用いてテーマ（Light / Dark / Forest）の動的切り替えを実装する（MUST）。
