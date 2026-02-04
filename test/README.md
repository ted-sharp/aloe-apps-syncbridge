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

## クリーンアップ

テストデータを削除する場合：

```powershell
# テストデータの削除
Remove-Item -Path "C:\TestData" -Recurse -Force

# ローカルアプリケーションデータの削除
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
Remove-Item -Path (Join-Path $localAppData "Company\SyncBridge") -Recurse -Force
```
