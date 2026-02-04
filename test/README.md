# SyncBridge テストリソース

このディレクトリには、SyncBridge のテストに使用するマニフェストとテストデータが含まれています。

## ディレクトリ構成

```
test/
├── manifests/           # テスト用マニフェストファイル
│   ├── minimal.json
│   ├── standard.json
│   ├── empty-apps.json
│   ├── network-share.json
│   ├── special-characters.json
│   └── README.md
├── setup-test-data.ps1  # テストデータセットアップスクリプト
└── README.md            # このファイル
```

## テスト環境のセットアップ

### 1. テストデータの作成

PowerShell を管理者権限で実行し、以下のコマンドを実行します：

```powershell
# デフォルトパス (C:\TestData\Source) にテストデータを作成
.\test\setup-test-data.ps1

# カスタムパスにテストデータを作成
.\test\setup-test-data.ps1 -TestDataRoot "D:\MyTestData"

# 既存のテストデータをクリーンアップして再作成
.\test\setup-test-data.ps1 -Clean

# DummyWpfアプリケーションをビルドして配置
.\test\setup-test-data.ps1 -BuildDummyWpf

# 実際の .NET 10 ランタイムをダウンロード (統合テスト用)
.\test\setup-test-data.ps1 -DownloadRealRuntime

# 完全な統合テスト環境のセットアップ (DummyWpf + 実際のランタイム)
.\test\setup-test-data.ps1 -BuildDummyWpf -DownloadRealRuntime

# クリーンアップして完全な統合テスト環境を再構築
.\test\setup-test-data.ps1 -Clean -BuildDummyWpf -DownloadRealRuntime

# ランタイムキャッシュを無効化 (常に最新版をダウンロード)
.\test\setup-test-data.ps1 -DownloadRealRuntime -UseRuntimeCache:$false
```

#### ランタイムダウンロード機能について

`-DownloadRealRuntime` スイッチを使用すると、Microsoft の公式 CDN から最新の .NET 10 Runtime (base + Windows Desktop) をダウンロードして統合テストに使用できます。

**機能の特徴:**
- 最新の安定版 .NET 10 ランタイムを自動検出してダウンロード
- ダウンロードしたファイルは `%TEMP%\SyncBridge-RuntimeCache\` にキャッシュされます
- SHA512 ハッシュ検証によるファイル整合性チェック (環境で利用可能な場合)
- ネットワークエラー時は 3 回まで自動リトライ
- キャッシュを利用すると 2 回目以降のセットアップが高速化 (約 5 秒)

**ダウンロードサイズ:**
- Base Runtime: 約 25 MB
- Windows Desktop Runtime: 約 37 MB
- 合計ダウンロード: 約 62 MB
- 展開後のサイズ: 約 200-250 MB
- 初回ダウンロード時間: 1-3 分程度 (回線速度により変動)

**ダウンロード内容:**
- Base .NET Runtime: `dotnet.exe` とコアフレームワーク (`Microsoft.NETCore.App`)
- Windows Desktop Runtime: WPF/WinForms フレームワーク (`Microsoft.WindowsDesktop.App`)
- これらにより、DummyWpf などのデスクトップアプリケーションを実際に起動できる完全な実行環境が構築されます

**キャッシュディレクトリ:**
デフォルトでは `%TEMP%\SyncBridge-RuntimeCache\` にダウンロードファイルがキャッシュされます。
カスタムキャッシュディレクトリを指定する場合:

```powershell
.\test\setup-test-data.ps1 -DownloadRealRuntime -RuntimeCacheDir "D:\MyCache"
```

### 2. マニフェストの配置

#### 方法A: ローカルアプリケーションデータに配置
```powershell
# テストしたいマニフェストをコピー
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$targetDir = Join-Path $localAppData "Company\SyncBridge"
New-Item -ItemType Directory -Path $targetDir -Force
Copy-Item "test\manifests\standard.json" -Destination (Join-Path $targetDir "manifest.json")
```

#### 方法B: 実行ファイルと同じディレクトリに配置
```powershell
# ビルド後の出力ディレクトリにコピー
Copy-Item "test\manifests\minimal.json" -Destination "src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\bin\Debug\manifest.json"
```

### 3. アプリケーションの実行

```powershell
# ビルドして実行
cd src
dotnet build
dotnet run --project Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge
```

## テストシナリオ

### 基本動作テスト
1. `minimal.json` を使用
2. アプリケーションを起動
3. アプリケーションが正しく表示されることを確認
4. 同期が成功することを確認

### 複数アプリケーション選択テスト
1. `standard.json` を使用
2. アプリケーションを起動
3. 3つのアプリケーションが表示されることを確認
4. 各アプリケーションを選択できることを確認
5. 選択したアプリケーションが正しく起動することを確認

### エラーハンドリングテスト
1. `empty-apps.json` を使用
2. アプリケーションリストが空の場合の動作を確認
3. 適切なエラーメッセージが表示されることを確認

### ネットワークパステスト
1. `network-share.json` を使用
2. UNC パスからの同期動作を確認
3. リトライ動作を確認

### 特殊文字処理テスト
1. `special-characters.json` を使用
2. パスに特殊文字が含まれる場合の処理を確認
3. 起動引数にスペースや特殊文字が含まれる場合の処理を確認

### 実際のアプリケーション起動テスト
1. `dummywpf.json` を使用
2. `setup-test-data.ps1 -BuildDummyWpf` で実際のWPFアプリケーションをビルド・配置
3. SyncBridgeを実行してDummyWpfが起動することを確認
4. DummyWpfのUIで以下を確認:
   - コマンドライン引数が正しく渡されているか
   - 環境変数が設定されているか
   - 作業ディレクトリが正しいか

## スキップパターンのテスト

`standard.json` を使用すると、以下のファイルが同期時にスキップされることを確認できます：
- `*.pdb` ファイル (デバッグシンボル)
- `*.log` ファイル (ログファイル)
- `*.tmp` ファイル (一時ファイル)
- `temp/*` ディレクトリ内のファイル

## 単体テストでの使用

```csharp
using System.IO;
using Newtonsoft.Json;
using Aloe.Apps.SyncBridgeLib.Models;

[TestClass]
public class ManifestTests
{
    [TestMethod]
    public void LoadMinimalManifest_Success()
    {
        // Arrange
        var manifestPath = Path.Combine("test", "manifests", "minimal.json");
        var json = File.ReadAllText(manifestPath);

        // Act
        var manifest = JsonConvert.DeserializeObject<SyncManifest>(json);

        // Assert
        Assert.IsNotNull(manifest);
        Assert.AreEqual("1.0", manifest.Version);
        Assert.AreEqual(1, manifest.Applications.Count);
        Assert.AreEqual("TestApp", manifest.Applications[0].AppId);
    }
}
```

## トラブルシューティング

### テストデータが見つからない
- `setup-test-data.ps1` を実行してテストデータを作成してください
- マニフェストの `sourceRootPath` がテストデータのパスと一致していることを確認してください

### マニフェストが読み込まれない
- マニフェストファイルが正しい場所に配置されているか確認してください
  - `%LocalAppData%\Company\SyncBridge\manifest.json`
  - または実行ファイルと同じディレクトリ

### 環境変数が展開されない
- `JsonManifestRepository.LoadManifest()` が環境変数を自動的に展開します
- テスト時は実際のパスに置き換えるか、`Environment.ExpandEnvironmentVariables()` を使用してください

### ランタイムダウンロード機能の変更履歴

**2026-02-04 (v2) - 完全ランタイムサポート追加:**
- Base Runtime と Windows Desktop Runtime の両方をダウンロードするように修正
- `dotnet.exe` が含まれるようになり、実際のアプリケーション起動が可能に
- ダウンロードサイズが約 37 MB → 約 62 MB に増加
- 統合テスト環境として完全に機能するように改善
- 両パッケージの抽出をマージすることで完全な実行環境を構築

**2026-02-04 (v1) - メタデータクエリ修正:**
以下の問題が修正されました:

1. **メタデータフィルタの修正**: Windows Desktop Runtime ZIP ファイルの検索条件を修正し、正しいファイル名パターン (`windowsdesktop-runtime-win-x64.zip`) で検索するようになりました

2. **バージョンプロパティの修正**: `release` ではなく `release-version` プロパティを使用するように修正

3. **フォールバック URL の更新**: メタデータクエリ失敗時のフォールバック URL を 10.0.0 から 10.0.2 に更新

4. **ハッシュ検証の改善**: `Get-FileHash` コマンドレットが利用できない環境でもエラーにならず、警告を表示してスキップするように改善

5. **診断情報の追加**: メタデータ取得時、ダウンロード URL 発見時に詳細ログを出力するように改善

### ランタイムダウンロードのトラブルシューティング

#### ネットワーク接続エラー
```
Error downloading runtime: The operation has timed out
```

**解決方法:**
- インターネット接続を確認してください
- プロキシやファイアウォールが Microsoft CDN へのアクセスをブロックしていないか確認してください
- `-UseRuntimeCache:$false` を使用してキャッシュを無効化してみてください

#### ハッシュ検証エラー
```
Hash mismatch! Expected: [hash]
```

**解決方法:**
- キャッシュされたファイルが破損している可能性があります
- キャッシュディレクトリ `%TEMP%\SyncBridge-RuntimeCache\` を手動で削除してください
- スクリプトを再実行してください

#### ディスク容量不足
```
Error: There is not enough space on the disk
```

**解決方法:**
- 少なくとも 500 MB の空き容量が必要です
- 不要なファイルを削除するか、カスタムキャッシュディレクトリを指定してください:
  ```powershell
  .\test\setup-test-data.ps1 -DownloadRealRuntime -RuntimeCacheDir "D:\MyCache"
  ```

#### Get-FileHash コマンドレットが利用できない
```
Hash verification skipped (Get-FileHash cmdlet not available).
```

**原因:**
- MSYS2 bash 環境や古い PowerShell 環境では `Get-FileHash` コマンドレットが利用できない場合があります

**影響:**
- ダウンロードファイルのハッシュ検証がスキップされますが、ダウンロード自体は正常に完了します
- 実用上問題はありませんが、ファイルの整合性チェックは手動で行うことをお勧めします

**解決方法 (オプション):**
- Windows PowerShell 5.1 以降または PowerShell Core を使用してスクリプトを実行してください

#### プロキシ環境でのダウンロード失敗

**解決方法:**
PowerShell のプロキシ設定を構成してください:
```powershell
# プロキシ設定
$proxy = [System.Net.WebRequest]::GetSystemWebProxy()
$proxy.Credentials = [System.Net.CredentialCache]::DefaultCredentials
[System.Net.WebRequest]::DefaultWebProxy = $proxy

# スクリプト実行
.\test\setup-test-data.ps1 -DownloadRealRuntime
```

## クリーンアップ

テストデータを削除する場合：

```powershell
# テストデータの削除
Remove-Item -Path "C:\TestData" -Recurse -Force

# ローカルアプリケーションデータの削除
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
Remove-Item -Path (Join-Path $localAppData "Company\SyncBridge") -Recurse -Force
```
