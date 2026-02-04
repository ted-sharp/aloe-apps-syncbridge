# テストマニフェスト

このディレクトリには、SyncBridge のテスト用マニフェストファイルが含まれています。

**フォーマット**: Windows INI形式（Win32 API でパース、外部依存なし）

## マニフェストファイル一覧

### minimal.ini
最小限の構成でのテスト用マニフェスト
- アプリケーション: 1つ
- 同期オプション: 最小限
- 用途: 基本的な動作確認

### standard.ini
標準的な複数アプリケーション構成のテスト用マニフェスト
- アプリケーション: 3つ
- 同期オプション: 複数のスキップパターンを含む
- 用途: 複数アプリの選択・同期動作確認

### dummywpf.ini
DummyWpf サンプルアプリケーションを使用したテスト用マニフェスト
- アプリケーション: DummyWpf (実際に動作するWPFアプリ)
- .NET 10.0 Runtime を使用
- 用途: 実際のアプリケーション起動動作の確認、コマンドライン引数の表示確認

## INI フォーマット例

```ini
[Manifest]
Version=1.0
SourceRootPath=C:\TestData\Source
LocalBasePath=%TEMP%\SyncBridgeTest\Minimal

[Runtime]
Version=8.0.0
RelativePath=runtime/dotnet-8.0.0

[SyncOptions]
RetryCount=3
TimeoutSeconds=300
SkipPatterns=*.pdb;*.log;*.tmp

[App.TestApp]
DisplayName=Test Application
Version=1.0.0
RelativePath=apps/TestApp-1.0.0
EntryDll=TestApp.dll
LaunchArgPattern=--mode=test
```

## 使用方法

### 手動テスト
1. テストしたいマニフェストを `%LocalAppData%\Company\SyncBridge\manifest.ini` にコピー
2. または、実行ファイルと同じディレクトリに `manifest.ini` として配置
3. SyncBridge を実行してテスト

```powershell
# setup-manifest.ps1 を使用して簡単にセットアップ
.\test\setup-manifest.ps1
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
