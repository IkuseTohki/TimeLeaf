# システムアーキテクチャ設計 (System Architecture)

## 1. 採用パターンと技術スタック (Tech Stack)

- **アーキテクチャ:** クリーンアーキテクチャ (Clean Architecture)
- **UI パターン:** MVVM (Model-View-ViewModel)
- **メインフレームワーク:** `.NET 10`, `WPF`
- **コアライブラリ:** `CommunityToolkit.Mvvm`, `Microsoft.Extensions.DependencyInjection`

## 2. レイヤー定義と依存方向 (Layer Definitions)

依存関係は常に内側（Domain Layer）に向かう（MUST）。外側のレイヤー（UIやファイルシステム等）の変更が、内側のビジネスロジックに影響を与えてはならない（MUST NOT）。

### 2.1 Domain Layer (Models)

ビジネスルールの中心であり、外部フレームワークやストレージの実装に一切依存しない。

- **Entities:** `Project`, `Task` 等のデータ構造と、それらに紐づく純粋なドメインロジック。
- **Repository Interfaces:** データの保存・取得に関する抽象定義（例: `IProjectRepository`）。

### 2.2 Application Layer (UseCases & Services)

アプリケーション固有のビジネスルール（シナリオ）と、状態の一元管理を担う。

- **UseCases:** 特定の機能操作を実行し、Repositoryインターフェース経由でEntityを操作する。
- **Services:** ドメインごとの状態を保持し、変更通知をUI層へ伝搬させるハブ機能（※詳細は `StateManagement.md` を参照）。

### 2.3 Presentation Layer (ViewModels)

UIのための状態保持とコマンド（操作）を提供する。

- **ViewModels:** `CommunityToolkit.Mvvm` の `ObservableObject` を継承する。
- Viewからの入力を UseCase / Service へ伝搬させ、Serviceからの変更通知イベントを購読して自身のプロパティを更新する。

### 2.4 Infrastructure & UI Layer (Repositories, Views)

外部環境（OS、ファイルシステム、UIフレームワーク）との接続を担う最外層。

- **Repositories:** Domain Layer のインターフェースを実装し、JSONファイルの読み書き、および `changes/` ディレクトリの Replay 処理を実行する。
- **Views:** XAMLによる描画。共通UI基盤（`LeafKit.UI`）を適用する。

## 3. 実装制約 (Implementation Constraints)

### 3.1 依存性の注入 (Dependency Injection)

- すべての ViewModel, UseCase, Service, Repository は DI コンテナ (`Microsoft.Extensions.DependencyInjection`) で管理する（MUST）。
- `new` 演算子を用いた依存オブジェクトの直接生成、および静的（`static`）な状態保持を禁止し、コンストラクタ注入（Constructor Injection）を徹底する（MUST NOT / MUST）。

### 3.2 非同期プログラミング (Asynchronous Programming)

- ファイル I/O を伴う Repository および UseCase のメソッドは、UIスレッドのブロックを回避するため常に `Task` または `IAsyncEnumerable` を返す（MUST）。
