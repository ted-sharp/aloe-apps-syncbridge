# テストマニフェスト

このディレクトリには、SyncBridge のテスト用マニフェストファイルが含まれています。

## マニフェストファイル一覧

### minimal.json
最小限の構成でのテスト用マニフェスト
- アプリケーション: 1つ
- 同期オプション: 最小限
- 用途: 基本的な動作確認

### standard.json
標準的な複数アプリケーション構成のテスト用マニフェスト
- アプリケーション: 3つ
- 同期オプション: 複数のスキップパターンを含む
- 用途: 複数アプリの選択・同期動作確認

### empty-apps.json
アプリケーションリストが空のマニフェスト
- アプリケーション: なし
- 用途: エラーハンドリングのテスト、空リスト処理の確認

### network-share.json
ネットワーク共有をソースとするマニフェスト
- ソースパス: UNCパス (\\\\test-server\\DeployShare)
- リトライ回数: 5回
- タイムアウト: 600秒
- 用途: ネットワーク経由の同期テスト

### special-characters.json
特殊文字を含むパス・名前のテスト用マニフェスト
- パスに括弧、スペース、アンダースコアを含む
- バージョンにプレビュー番号を含む
- 用途: パス処理、文字列エスケープの確認

### dummywpf.json
DummyWpf サンプルアプリケーションを使用したテスト用マニフェスト
- アプリケーション: DummyWpf (実際に動作するWPFアプリ)
- .NET 10.0 Runtime を使用
- 用途: 実際のアプリケーション起動動作の確認、コマンドライン引数の表示確認

## 使用方法

### 手動テスト
1. テストしたいマニフェストを `%LocalAppData%\\Company\\SyncBridge\\manifest.json` にコピー
2. または、実行ファイルと同じディレクトリに `manifest.json` として配置
3. SyncBridge を実行してテスト

### 自動テスト
各マニフェストを使用した単体テストで利用します。

```csharp
// テストコード例
var repository = new JsonManifestRepository();
var manifest = JsonConvert.DeserializeObject<SyncManifest>(
    File.ReadAllText("test/manifests/minimal.json")
);
```

## 環境変数の展開

マニフェストでは以下の環境変数が使用できます：
- `%TEMP%` - 一時フォルダ
- `%LocalAppData%` - ローカルアプリケーションデータフォルダ
- その他の Windows 環境変数

## 注意事項

- テスト用マニフェストのパスは実際には存在しない可能性があります
- ネットワークパステストは適切なネットワーク環境が必要です
- 本番環境では使用しないでください
