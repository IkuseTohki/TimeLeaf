# テスト方針 (Test Strategy)

## 1. 基本原則 (Core Principles)

### 1.1. スモールステップとTDD

すべての実装は「最小単位のテスト（Red）」から開始し、実装（Green）、リファクタリングのサイクルを回すスモールステップ戦略を厳守する。

### 1.2. 1クラス・1テストファイルの原則

- 原則として、**1つのプロダクトクラスに対して1つのテストクラス（およびファイル）**を作成する。
- 特定の機能（例: 保存、Replay）ごとにファイルを分割せず、対象クラス（SUT: System Under Test）のすべての振る舞いをひとつのテストファイルに集約する。
- テストファイル名は `[対象クラス名]Tests.cs` とし、物理ディレクトリ構造もプロダクトコードと一致させる。

### 1.3. カプセル化の尊重

- テストのために実装詳細（非公開メソッドや内部的なキー生成ロジック等）を外部へ公開してはならない。
- 実装詳細の知識が複数のクラスに漏洩することを防ぐため、ロジックをカプセル化したインターフェース（例: `IProjectStorageCache`）を導入し、その公開メソッドを介してテストを行う。

---

## 2. ユニットテスト (Unit Testing)

- **目的:** 個々のコンポーネントが単体で、意図したロジックにしたがって正しく動作することを保証する。
- **対象範囲:**
  - `UseCase` (ドメインロジック)
  - `ViewModel` (UI制御ロジック)
  - `Repositories.FileSystem` 内の純粋ロジック部（Serializer, Replayer, Cache等）
  - `Utilities`, `ValueObjects`
- **ルール:**
  - I/O（ファイル、ネットワーク）、外部サービス、Dispatcher等には直接依存せず、Mock（Moq）を使用して分離する。
  - とくにキャッシュや状態復元（Replay）のロジックは、初期状態から確定状態までの遷移が正しいキーで管理されているかを厳密に検証する。
- **フレームワーク:** MSTest, Moq

---

## 3. インテグレーションテスト (Integration Testing)

- **目的:** 複数のコンポーネント、および外部リソース（ファイルシステム等）との連携が正しく機能することを保証する。
- **対象範囲:**
  - `Repository` (実際のファイルI/Oを含む操作)
  - `Service` 間のイベント伝播
- **ルール:**
  - 実際のファイルシステム（`Path.GetTempPath()` を利用した一時フォルダ）を使用する。
  - 物理的な競合（同名ファイルエラー）や、Last Writer Wins (LWW) に基づくデータのマージが正しく行われるかを実地で検証する。

---

## 4. テストの命名規則・フォーマット

### 4.1. メソッド命名

テストメソッド名は、何がテストされているかを仕様として読み取れるよう、以下の3要素で構成する（MUST）。

- **フォーマット:** `MethodName_Condition_ExpectedResult`
  - `MethodName`: テスト対象のメソッド名または機能名。
  - `Condition`: テストを実行する際の前提条件（Given / When）。
  - `ExpectedResult`: 期待される結果（Then）。
- **例:** `ReplayChanges_WhenTimestampsAreSame_SortsByUserId`
- **例:** `CompleteTaskCommand_WhenDependenciesNotMet_CanExecuteIsFalse`

### 4.2. テスト構造 (AAAパターン)

すべてのテストメソッドは、準備・実行・検証の3つのフェーズを明確に分離し、コード内にコメントブロックで区切って記述する（MUST）。

- **Arrange (準備):** 状態の設定とMockの構築。
- **Act (実行):** テスト対象メソッドの呼び出し。
- **Assert (検証):** 結果の確認。

```csharp
[Fact]
public void CalculateProgress_WhenAllChildTasksAreDone_Returns100()
{
    // Arrange
    var service = new TaskService();
    // ...

    // Act
    var result = service.CalculateProgress(containerId);

    // Assert
    result.Should().Be(100);
}
```

### 4.3. テスト観点の明記

- 各テストメソッドには、**「何を確認したいのか」というテスト観点**を XML ドキュメントコメント (`<summary>`) として必ず記載する。
- 例: `/// テスト観点: 重複するコメント保存時に、キャッシュヒットして物理書き込みがスキップされることを確認する。`
