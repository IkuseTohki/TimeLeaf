# ファイルストレージとマッピング仕様 (File Storage & Mapping)

本ドキュメントは、`DataModel.md` で定義された論理エンティティを、コンフリクトを最小化しつつどのようにファイルシステムへシリアライズし、同期・永続化するか（物理マッピング）を規定する。

## 1. 物理構造とファイル階層 (Physical Structure)

データは以下のディレクトリ構造を厳守して配置・生成する（MUST）。

```text
{TimeLeaf_Data_Root}/
├── users/
│   ├── {UserId}.json              # UserProfile エンティティの保存先
│   └── {UserId}.png
└── {ProjectId}_{ProjectName}/
    ├── .project                   # プロジェクトの不変メタデータ
    ├── changes/
    │   ├── {yyyyMMdd_HHmmss_fff}_{UserId}_{GUID}_Project_Basic.json
    │   └── {TaskId}/              # タスク・コンテナ・コメントの履歴格納ディレクトリ
    │       ├── {yyyyMMdd_HHmmss_fff}_{UserId}_{GUID}_Task_Planning.json
    │       ├── {yyyyMMdd_HHmmss_fff}_{UserId}_{GUID}_Container_Planning.json
    │       └── ...
```

- **コンテナの物理配置:** 子タスクを束ねる「コンテナ」であっても、ファイルシステム上は一般タスクと同一の物理構造（`changes/{ContainerId}/`）にフラットに配置する。ディレクトリの入れ子によって親子関係を表現してはならない（MUST NOT）。

## 2. 変更履歴ファイル命名規則 (Commit Naming Convention)

- **フォーマット:** `{yyyyMMdd_HHmmss_fff}_{UserId}_{GUID}_{Category}.json` (MUST)
  - **Timestamp (`yyyyMMdd_HHmmss_fff`):** JST。適用の時系列を決定する。
  - **GUID:** 同一ミリ秒内の衝突を回避する識別子。
  - **Category:** 以下「カテゴリ分割マッピング」で定義する種別。

## 3. 同期とコンフリクト解決 (Replay & LWW)

- 同一カテゴリに対する複数の変更履歴が存在する場合、**Last Writer Wins (LWW)** を適用し、ファイル名のタイムスタンプがもっとも新しいものを「正」としてメモリ上に上書き（Replay）する（MUST）。
- 同着の場合は `{UserId}` の辞書順（昇順）で後にくるものを勝者とする（MUST）。

## 4. カテゴリ分割とプロパティ・マッピング (Category Mapping)

コンフリクトを局所化するため、`DataModel.md` のプロパティを以下のカテゴリ（Category）単位でグループ化して1つのJSONファイルとしてシリアライズする（MUST）。

### 4.1 Project プロパティのマッピング

- **[Category: `Project_Basic`]** (更新頻度: 中)
  - `Name`
  - `Status`
  - `Category`
  - `ProgressCalculationMethod`
- **[Category: `Project_Description`]** (更新頻度: 低)
  - `Description`
- **[Category: `Project_Milestones`]** (更新頻度: 低)
  - `Milestones`
  - `Tags`
- **[Category: `Project_Members`]** (更新頻度: 低)
  - `AssignedUserIds`
- **[Category: `Project_SortOrder`]** (更新頻度: 中 / 一括更新)
  - **キー:** `OrderedIds` (List<Guid>)
  - _マッピング規則:_ `DataModel` における `Project.Containers` リストの現在の並び順を、コンテナIDの配列（`OrderedIds`）として抽出してシリアライズする。デシリアライズ時は、この配列順にコンテナのインスタンスをリストへ配置する。
  - _設計意図:_ コンテナのドラッグ＆ドロップ（並べ替え）による更新を他のメタデータと分離し、コンフリクトを最小化するため。
- ※ `Id`, `CreatedAt`, `CreatedBy` は不変メタデータとしてルート直下の `.project` ファイルにのみ保存する。

### 4.2 Task (およびコンテナ) プロパティのマッピング

- **[Category: `Task_Progress`]** (更新頻度: 高)
  - `Status`
  - `ProgressPercentage`
  - `ActualStartDate`
  - `ActualEndDate`
  - `ActualHours`
  - `CompletionReason`
- **[Category: `Task_Planning`]** (更新頻度: 中)
  - `Name`
  - `Priority`
  - `ParentTaskId`
  - `Constraints`
  - `PlannedStartDate`
  - `PlannedEndDate`
  - `Deadline`
  - `RequiredDays`
  - `InitialEstimatedHours`
  - `RevisedEstimatedHours`
- **[Category: `Task_Assignees`]** (更新頻度: 低)
  - `AssigneeId`
  - `ReviewerId`
  - `WatcherIds`
  - `RelatedTaskIds`
- **[Category: `Task_Description`]** (更新頻度: 低)
  - `Description`

### 4.3 Container プロパティのマッピング

コンテナは、タスクとは異なる専用のカテゴリ名を持つことで識別される（MUST）。コンテナは進捗やアサイン情報をファイルとして持たない。

- **[Category: `Container_Planning`]** (更新頻度: 中)
  - `Name`
  - `ParentTaskId`
  - `Constraints`
  - `PlannedStartDate`
  - `PlannedEndDate`
  - `Deadline`
  - `RequiredDays`
- **[Category: `Container_Relations`]** (更新頻度: 低) ★タスク側と粒度を統一
  - `WatcherIds`
  - `RelatedTaskIds`
- **[Category: `Container_Description`]** (更新頻度: 低)
  - `Description`

### 4.4 削除マーカー (Tombstone Marker)

エンティティ（タスクおよびコンテナ）の論理削除は、独立した汎用的な削除マーカーファイルによって表現する（MUST）。

- **[Category: `Deleted`]**
  - **ファイル名フォーマット:** `{yyyyMMdd_HHmmss_fff}_{UserId}_{GUID}_Deleted.json`
  - ファイルの内容は空オブジェクト `{}` とする。
  - _設計意図:_ Replay（起動時）において、このカテゴリを検知しそれが最新であった場合、システムは「それがタスクであったかコンテナであったか」を問わず、該当IDの他のすべてのJSONファイルのパースをスキップし、パフォーマンスを最適化する。

### 4.5 削除時のカスケード規則

親コンテナが削除（`_Deleted.json` マーカーが生成）された場合、システムはReplay時にそのコンテナを `ParentTaskId` とするすべての子孫エンティティ（タスクおよびコンテナ）も「削除されたもの」として扱い、メモリ上に展開してはならない（MUST）。
（※削除されたコンテナ内の子エンティティに対して、個別にマーカーファイルを生成する必要はない）
