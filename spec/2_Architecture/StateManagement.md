# 状態管理仕様 (State Management)

※ 参照: ADR-0006

## 1. サービス層の集約 (Service Hub)

`Repository`（永続化）と `ViewModel`（表示）の間に、リアクティブな状態管理ハブ（Service層）を導入する。

- ドメインごとにサービスインターフェース（例: `IUserService`, `ITaskService`）を定義し、データの取得・保存を一手に引き受ける（MUST）。
- `ViewModel` は `Repository` を直接参照せず、必ずServiceを介してデータにアクセスする（MUST NOT / MUST）。

## 2. インメモリ・キャッシュと変更検知 (In-Memory Cache & Detection)

- Service内部にスレッドセーフなキャッシュ（`ConcurrentDictionary` 等）を保持し、不要なファイルI/Oを削減する（MUST）。
- Infrastructure層は `FileSystemWatcher` を用いて同期ディレクトリを常時監視する。外部からのファイル変更を検知した際、Serviceのキャッシュを更新し、C#イベント（`Action<T>` または `IObservable<T>`）として変更通知を公開する（MUST）。

## 3. リアクティブなUI更新 (Reactive UI Updates)

- `ViewModel` は初期化時にServiceからデータを取得するとともに、Serviceが公開する変更通知イベントを購読する（MUST）。
- イベントを受信した `ViewModel` は、自身のプロパティを更新し `INotifyPropertyChanged` を発火させることでUIを再描画する。

### 3.1 スレッドセーフティに関する制約

- `FileSystemWatcher` に起因するバックグラウンドスレッドからのイベントで `ObservableCollection` を操作するとクラッシュする。コレクションの更新は、必ず `Application.Current.Dispatcher` 等を用いてUIスレッドにディスパッチする（MUST）。

### 3.2 メモリリークの防止

- `ViewModel` の破棄時（画面遷移やダイアログのクローズ時）には、必ずServiceのイベント購読を解除（`Dispose` または `-=`）する（MUST）。
