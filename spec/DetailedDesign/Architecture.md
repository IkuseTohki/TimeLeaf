# アーキテクチャ設計書

## 1. 採用パターン

- **Architecture**: クリーンアーキテクチャ (Clean Architecture)
- **UI Pattern**: MVVM (Model-View-ViewModel)
- **Framework**: .NET 10, WPF, CommunityToolkit.Mvvm

## 2. レイヤー定義と依存方向

依存関係は常に **内側（Domain）** に向かい、外側のレイヤーが変更されても内側のロジックに影響を与えないように設計する。

### 2.1. Domain Layer (Models)

- **役割**: ビジネスルールの中心。外部の何者にも依存しない。
- **内容**:
  - `Entities`: Project, Task 等のデータ構造と、それらに紐づく純粋な計算ロジック。
  - `Repository Interfaces`: データの保存・取得に関する抽象定義。

### 2.2. Application Layer (UseCases)

- **役割**: 特定のアプリケーション機能を実行するための操作手順（シナリオ）を定義。
- **内容**:
  - 各機能の `UseCase` クラス。
  - Repository 経由でデータを取得し、Entity を操作して結果を返す。

### 2.3. Presentation Layer (ViewModels)

- **役割**: UI のための状態保持とコマンドの提供。
- **内容**:
  - `ObservableObject` を継承した ViewModel。
  - View からの入力を UseCase に橋渡しし、結果を UI 向けに整形して通知する。

### 2.4. Infrastructure / UI Layer (Repositories, Views)

- **役割**: 外部環境（OS、ファイルシステム、UIフレームワーク）との接続。
- **内容**:
  - `Repositories`: JSON ファイルの読み書き、`changes/` フォルダの Replay 処理の実装。
  - `Views`: XAML による描画。`LeafKit.UI` の適用。

## 3. 実装上の重要ルール

### 3.1. 依存性の注入 (DI)

- すべての ViewModel, UseCase, Repository は DI コンテナ (`Microsoft.Extensions.DependencyInjection`) で管理する。
- 静的クラスや `new` による依存オブジェクトの直接生成を禁止し、コンストラクタ注入を用いる。

### 3.2. 非同期プログラミング

- ファイル I/O を伴う Repository および UseCase のメソッドは、常に `Task` または `IAsyncEnumerable` を返し、UI スレッドをブロックしない。

### 3.3. フォルダベースのデータ同期

- Repository 層は `FileSystemWatcher` を用いて同期フォルダの変更を検知し、ドメインイベントを介して ViewModel に状態更新を通知する。
