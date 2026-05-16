# データモデル仕様 (Data Model)

本システムで取り扱うエンティティの論理スキーマを定義する。
本ドキュメントは純粋なドメインモデルの定義であり、物理的な保存形式には依存しない。
日時はすべてJST（日本標準時）に統一する（MUST）。

## 1. Project (プロジェクト)

プロジェクト全体を管理するエンティティ。

- **Id (`ProjectId`):** Guid (MUST)
- **作成日時 (`CreatedAt`):** DateTime (MUST / ミリ秒精度 `fff` 必須)
- **作成者Id (`CreatedBy`):** Guid (MUST)
- **名前 (`Name`):** string (MUST)
- **概要 (`Description`):** string (Markdown対応 / MAY)
- **分類 (`Category`):** string (`開発`, `機能追加`, `改善`, `不具合対応` 等)
- **状態 (`Status`):** string (`準備中`, `進行中`, `完了`, `一時停止`) (MUST / デフォルト: `準備中`)
- **マイルストーン (`Milestones`):** List<Milestone> (重要な節目となる日付とラベルのリスト)
- **タグ (`Tags`):** List<string> (横断検索用のラベル)
- **開始予定日 (`PlannedStartDate`):** DateTime?
- **終了予定日 (`PlannedEndDate`):** DateTime?
- **進捗定義 (`ProgressCalculationMethod`):** string (`TaskCount` または `Workload`) (MUST / デフォルト: `TaskCount`)

**【メンバー管理とアクセス制御 (Members & Access Control)】**

- **アサイン済ユーザー (`AssignedUserIds`):** List<Guid> (当該プロジェクトに参加しているメンバーのUserIdリスト)
- **アクセス権限制約:** アプリケーションは、以下の権限ルールを適用してUIの編集制御を行わなければならない（MUST）。
  - 現在ログインしているユーザーの `UserId` が `AssignedUserIds` に含まれている場合: **フルコントロール**（タスクの作成・編集・削除等すべての操作が可能）。
  - 含まれていない（非アサイン）場合: **閲覧専用 (Read-Only)**（プロジェクトダッシュボードやタスクリストの閲覧は可能だが、すべての編集操作はロック・非表示化される）。
  - ※プロジェクトの作成者（`CreatedBy`）は、作成時に自動的に `AssignedUserIds` に追加される。

**【プロジェクトの構造 (Project Structure)】**

- **コンテナリスト (`Containers`):** `List<Container>`
  - プロジェクト直下に属するコンテナの実体を保持するリスト。
  - **このリストの並び順（インデックス順）が、そのままUI上でのコンテナの表示順となる。**並び順を管理するためだけの専用のID配列プロパティはドメインモデルとして保持しない。
- **順序のフォールバック規則 (Fallback Rule):**
  インフラ層からのデータロード（Replay）時、ファイル同期のコンフリクト等により「データとしては存在するが、順序データから漏れているコンテナ」が発生しうる。
  これを救済するため、システムが `Containers` リストをインメモリで再構築する際、順序未定義のコンテナを発見した場合は**リストの末尾に、作成日時 (`CreatedAt`) の昇順で自動的に追加**してロードしなければならない（MUST）。

## 2. 作業エンティティ (Tasks & Containers)

具体的な作業単位、およびそれらを束ねる器となるエンティティ。
両者は明確に異なるモデルとして定義され、それぞれ固有のプロパティと振る舞いを持つ。

### 2.1 Task (タスク)

ユーザーが直接作業と実績入力を行う末端のエンティティ。

**基本情報**

- **Id (`TaskId`):** Guid (MUST)
- **プロジェクトId (`ProjectId`):** Guid (MUST)
- **親Id (`ParentTaskId`):** Guid? (階層構造用。所属するコンテナのId)
- **名前 (`Name`):** string (MUST)
- **詳細説明 (`Description`):** string (Markdown対応 / MAY)
- **優先度 (`Priority`):** int または enum (緊急度・重要度)
- **作業担当者 (`AssigneeId`):** Guid?
- **レビュアー (`ReviewerId`):** Guid?
- **ウォッチャー (`WatcherIds`):** List<Guid>

**【スケジュールと工数】**

- **開始予定日 (`PlannedStartDate`) / 終了予定日 (`PlannedEndDate`):** DateTime?
- **期限 (`Deadline`):** DateTime? (移動不可能な最終納期)
- **必要日数 (`RequiredDays`):** double
- **見積工数\_初回 (`InitialEstimatedHours`):** double
- **見積工数\_修正後 (`RevisedEstimatedHours`):** double
- **実績工数 (`ActualHours`):** double (デフォルト: `0`)

**【進捗と状態】**

- **状態 (`Status`):** string (`未着手`, `着手中`, `レビュー待ち`, `完了`) (MUST / デフォルト: `未着手`)
- **進捗率 (`ProgressPercentage`):** int (0〜100 / デフォルト: `0`)
- **開始日 (`ActualStartDate`) / 終了日 (`ActualEndDate`):** DateTime?
- **完了理由 (`CompletionReason`):** string (状態が完了の場合のみ設定可能)

**【制約と関係性】**

- **制約 (`Constraints`):** List<TaskConstraint> (先行タスクId、FS/SS等の型、猶予日数、理由を保持)
- **関連タスク (`RelatedTaskIds`):** List<Guid> (影響範囲の参照)

### 2.3 共通プロパティ (Common Properties)

タスクとコンテナは、共通の基底モデル (`ProjectWorkItem`) として以下の共通的な振る舞いや基本属性を保持する。
(※表示順序は、リストの物理的な並び順および Project_SortOrder に定義された ID リストの順序によって決定される)

### 2.2 Container (コンテナ)

タスクを束ねる「入れ物」であり、マクロな計画や全体構造の構築に特化するエンティティ。
タスクとは異なり、優先度や担当者、実績入力項目などのプロパティを保持しない。

**【保持・編集可能な情報 (Editable)】**

- **基本情報:**
  - **Id (`ContainerId`):** Guid (MUST)
  - **プロジェクトId (`ProjectId`):** Guid (MUST)
  - **親Id (`ParentTaskId`):** Guid? (コンテナが別のコンテナに属する場合のId)
  - **名前 (`Name`):** string (MUST)
  - **詳細説明 (`Description`):** string (Markdown対応 / MAY)
- **トップダウン計画:** 開始/終了予定日 (`PlannedStartDate`, `PlannedEndDate`), 期限 (`Deadline`), 必要日数
- **マクロな関係性:** 制約 (`Constraints`), 関連タスク (`RelatedTaskIds`)
- **関心層:** ウォッチャー (`WatcherIds`)

**【自動計算となる情報 (Calculated / Read-Only)】**
以下のプロパティはコンテナとしての状態を表すが、ファイルには保存されず、すべて内部の子タスク（`ParentTaskId` が自身のIdであるタスクまたはコンテナ）の実績から、積み上げ計算によって動的に導出されなければならない（MUST）。

- **`ProgressPercentage` (進捗率):** プロジェクトの進捗定義に基づく。
- **`ActualHours` (実績工数):** 子タスクの実績工数の合計。
- **`ActualStartDate` (実績開始日):** 子の中でもっとも早い開始日。
- **`ActualEndDate` (実績終了日):** すべての子が完了している場合、もっとも遅い終了日。
- **`Status` (状態):** 子の状態で決定（すべて未着手なら未着手、1つでも着手中なら着手中等）。

## 3. UserProfile (ユーザー)

- **Id (`UserId`):** Guid (MUST)
- **表示名 (`DisplayName`):** string (MUST)
- **テーマカラー (`ThemeColor`):** string (SHOULD)
- **アイコンパス (`IconPath`):** string (MAY)
- **更新日時 (`UpdatedAt`):** DateTime (MUST)

## 4. Comment (インライン・コメント)

- **Id (`CommentId`):** Guid (MUST)
- **タスクId (`TaskId`):** Guid (MUST)
- **投稿者Id (`AuthorId`):** Guid (MUST)
- **内容 (`Content`):** string (Markdown対応 / MUST)
- **添付ファイル (`Attachments`):** List<string> (ファイルパス・リンク)
