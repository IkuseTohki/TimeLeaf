# テーマ哲学とカラートークン (Theme Philosophy & Colors)

## 1. 世界観の定義 (Philosophy)

本アプリケーションにおけるUIデザインの根本哲学は、**「ツールは主役ではなく、ユーザーのコンテンツを引き立てる『静謐な背景』であるべき」**という思想に基づく。
UIの自己主張を極限まで抑え、ユーザーの視線と認知リソースを「プロジェクトのタスク」そのものに集中させる設計を徹底する（MUST）。

## 2. Soft Emphasis 規約 (Soft Emphasis Rule)

強い原色や太い枠線による暴力的な視線誘導を避け、「余白」と「微細な色の変化」によって状態（State）を伝える規約を適用する（MUST）。

- **`StateHoverBrush`:** インタラクションの予兆を示す低不透明度のグレー。
- **`EmphasisLowBrush`:** 選択状態等を示すアクセント色の透過ブラシ（Light: 15% / Dark: 35%）。
- **`EmphasisHighBrush`:** 強い強調を示すアクセント色 100%。

## 3. 主要カラートークンとテーマ別Hex値 (Color Token Reference)

UIコンポーネントの色指定には、物理的なHex値ではなく以下のトークン名を使用する（MUST）。実装時のリファレンスとして、各テーマにおける具体的なカラーパレットを定義する。

### テーマの設計コンセプト

- **Light Theme:** 清潔感と信頼性を重視し、長時間の作業でも目が疲れない白基調のパレット。
- **Dark Theme:** 没入感と疲労軽減を重視する。コントラストが強すぎる純黒（`#000000`）を避け、深いグレーを基調とする。
- **Forest Theme:** 「オーガニック」な世界観を体現する。背景に温かみのあるベージュ、アクセントに深い森の緑を用いる。

### トークン・マトリクス

| トークン名 (`Token Name`) | セマンティクス（用途）                             | Light Theme                 | Dark Theme                     | Forest Theme                  |
| :------------------------ | :------------------------------------------------- | :-------------------------- | :----------------------------- | :---------------------------- |
| **`SurfaceBaseBrush`**    | ウィンドウのもっとも奥の背景。静謐な空間のベース。 | `#F3F4F6`<br>_(Off-White)_  | `#121212`<br>_(Deep Gray)_     | `#F9F6F0`<br>_(Warm Beige)_   |
| **`SurfaceLayer1Brush`**  | カードやパネルの背景。コンテンツが乗るメイン領域。 | `#FFFFFF`<br>_(Pure White)_ | `#1E1E1E`<br>_(Elevated Gray)_ | `#FFFFFF`<br>_(Pure White)_   |
| **`BrandPrimaryBrush`**   | プライマリアクション、強調を示すブランド色。       | `#2563EB`<br>_(Calm Blue)_  | `#3B82F6`<br>_(Bright Blue)_   | `#2D5A27`<br>_(Forest Green)_ |
| **`TextPrimaryBrush`**    | 主要なテキスト。真っ黒を避けた高コントラスト色。   | `#111827`<br>_(Near Black)_ | `#F9FAFB`<br>_(Off-White)_     | `#2C362B`<br>_(Deep Olive)_   |
| **`TextSecondaryBrush`**  | 補足テキストやメタデータ。視線を奪わない文字色。   | `#6B7280`<br>_(Gray)_       | `#9CA3AF`<br>_(Muted Gray)_    | `#737A71`<br>_(Moss Gray)_    |
| **`OutlineBaseBrush`**    | 要素の境界線やセパレーター。極めて控えめな線。     | `#E5E7EB`<br>_(Light Gray)_ | `#3F3F46`<br>_(Dark Border)_   | `#E2DCD0`<br>_(Warm Border)_  |

### 3.1 Forestテーマにおける特記事項

Forestテーマの `TextPrimaryBrush` (`#2C362B`) は、純粋なグレーではなく、微かに緑と茶色（アースカラー）を帯びた色を使用する。これにより、`SurfaceBaseBrush` のベージュとの間に自然な調和（オーガニックな視覚的温度感）を生み出す（SHOULD）。
