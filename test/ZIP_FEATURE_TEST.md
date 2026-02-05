# ZIP展開機能 テストガイド

## 概要

このドキュメントは、SyncBridgeに実装されたZIP展開機能のテスト手順を説明します。

## 実装内容

### 追加されたファイル

1. **Services/IZipExtractor.cs** - ZIP展開インターフェース
2. **Services/ZipExtractor.cs** - ZIP展開実装
3. **test/manifests/zip-test.ini** - テスト用マニフェスト
4. **test/setup-zip-test-data.ps1** - テストデータセットアップスクリプト

### 修正されたファイル

1. **Models/SyncManifest.cs** - `ZipFileName` プロパティ追加
2. **Repositories/IniManifestRepository.cs** - ZIP設定読み込み
3. **Services/SyncOrchestrator.cs** - ZIP対応同期処理
4. **Program.cs** - 依存性注入
5. **Aloe.Apps.SyncBridgeLib.csproj** - アセンブリ参照追加

## テストデータのセットアップ

### 1. テストデータ作成

PowerShellで以下を実行:

```powershell
.\test\setup-zip-test-data.ps1
```

オプション:
- `-Clean`: 既存のテストデータを削除してから作成
- `-TestDataRoot "パス"`: テストデータの作成場所を指定（デフォルト: `C:\TestData\SyncBridge`）

例:
```powershell
# クリーンインストール
.\test\setup-zip-test-data.ps1 -Clean

# カスタムパス
.\test\setup-zip-test-data.ps1 -TestDataRoot "D:\MyTestData"
```

### 2. 作成されるファイル構造

```
C:\TestData\SyncBridge\
├── dotnet-test.zip              (Runtime ZIP)
├── TestApp.zip                  (Application ZIP)
├── runtime\
│   └── dotnet-test\
│       ├── dotnet.exe
│       ├── runtime.dll
│       └── README.txt
└── apps\
    └── TestApp\
        ├── TestApp.dll
        ├── TestApp.deps.json
        └── config.json
```

## テストシナリオ

### シナリオ1: 初回展開

**目的**: ZIPファイルから初めてファイルを展開する

**手順**:
1. テストデータをセットアップ
2. ローカルの同期先ディレクトリが存在しないことを確認
   ```powershell
   Remove-Item -Path "$env:TEMP\SyncBridgeTest" -Recurse -Force -ErrorAction SilentlyContinue
   ```
3. テスト用マニフェストをコピー
   ```powershell
   Copy-Item test\manifests\zip-test.ini src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\bin\Debug\manifest.ini -Force
   ```
4. SyncBridgeを実行
   ```powershell
   dotnet run --project src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge -- --sync --console
   ```

**期待される結果**:
- コンソールに「ZIP同期戦略: InitialExtraction」と表示される
- ZIPファイルが展開される
- マーカーファイル（`.zip-extracted`）が作成される
- 展開されたファイル数が表示される

**確認**:
```powershell
# ファイルが展開されているか確認
Get-ChildItem "$env:TEMP\SyncBridgeTest\runtime\dotnet-test"
Get-ChildItem "$env:TEMP\SyncBridgeTest\apps\TestApp"

# マーカーファイルの内容確認
Get-Content "$env:TEMP\SyncBridgeTest\runtime\dotnet-test\.zip-extracted"
Get-Content "$env:TEMP\SyncBridgeTest\apps\TestApp\.zip-extracted"
```

---

### シナリオ2: 2回目実行（変更なし）

**目的**: ZIPファイルに変更がない場合はスキップされる

**手順**:
1. シナリオ1の続きから
2. SyncBridgeを再実行
   ```powershell
   dotnet run --project src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge -- --sync --console
   ```

**期待される結果**:
- コンソールに「ZIP同期戦略: Skip」と表示される
- ファイル更新数が0
- 処理が高速に完了

---

### シナリオ3: ZIP更新（再展開）

**目的**: ZIPファイルが更新された場合に再展開される

**手順**:
1. シナリオ2の続きから
2. ZIPファイルのタイムスタンプを更新
   ```powershell
   (Get-Item C:\TestData\SyncBridge\dotnet-test.zip).LastWriteTime = Get-Date
   (Get-Item C:\TestData\SyncBridge\TestApp.zip).LastWriteTime = Get-Date
   ```
3. SyncBridgeを実行
   ```powershell
   dotnet run --project src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge -- --sync --console
   ```

**期待される結果**:
- コンソールに「ZIP同期戦略: ReExtraction」と表示される
- 既存ディレクトリが削除される
- ZIPファイルが再展開される
- マーカーファイルが更新される

**確認**:
```powershell
# マーカーファイルのタイムスタンプが更新されているか確認
Get-Content "$env:TEMP\SyncBridgeTest\runtime\dotnet-test\.zip-extracted"
```

---

### シナリオ4: フォルダ差分同期

**目的**: ZIPに変更がなく、ネットワーク共有上のフォルダに変更がある場合は差分同期

**手順**:
1. シナリオ2の続きから
2. ネットワーク共有上のフォルダにファイルを追加
   ```powershell
   Set-Content -Path "C:\TestData\SyncBridge\runtime\dotnet-test\newfile.txt" -Value "This is a new file" -Encoding ASCII
   Set-Content -Path "C:\TestData\SyncBridge\apps\TestApp\newconfig.json" -Value '{"newSetting": "value"}' -Encoding ASCII
   ```
3. SyncBridgeを実行
   ```powershell
   dotnet run --project src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge -- --sync --console
   ```

**期待される結果**:
- コンソールに「ZIP同期戦略: FolderSync」と表示される
- 新しいファイルが同期される
- 既存のファイルは変更されない（タイムスタンプチェック）

**確認**:
```powershell
# 新しいファイルが同期されているか確認
Test-Path "$env:TEMP\SyncBridgeTest\runtime\dotnet-test\newfile.txt"
Test-Path "$env:TEMP\SyncBridgeTest\apps\TestApp\newconfig.json"
```

---

### シナリオ5: エラーハンドリング（ZIP欠落）

**目的**: ZIPファイルが見つからない場合のエラー処理

**手順**:
1. ZIPファイルを一時的にリネーム
   ```powershell
   Rename-Item C:\TestData\SyncBridge\dotnet-test.zip dotnet-test.zip.bak
   ```
2. SyncBridgeを実行
   ```powershell
   dotnet run --project src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge -- --sync --console
   ```
3. ZIPファイルを元に戻す
   ```powershell
   Rename-Item C:\TestData\SyncBridge\dotnet-test.zip.bak dotnet-test.zip
   ```

**期待される結果**:
- エラーメッセージ「ZIPファイルが見つかりません」が表示される
- プログラムが適切に終了する

---

### シナリオ6: 従来のフォルダ同期（後方互換性）

**目的**: `ZipFileName` が指定されていない場合は従来通りフォルダ同期

**手順**:
1. 従来のマニフェストを使用（`ZipFileName` なし）
   ```powershell
   Copy-Item test\manifests\minimal.ini src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\bin\Debug\manifest.ini -Force
   ```
2. SyncBridgeを実行

**期待される結果**:
- ZIP関連のメッセージが表示されない
- 従来通りフォルダ同期が実行される

---

## トラブルシューティング

### 問題: 「ZIPファイルが破損しています」エラー

**原因**: ZIPファイルが不完全または破損している

**対処法**:
```powershell
.\test\setup-zip-test-data.ps1 -Clean
```

### 問題: 「アクセスが拒否されました」エラー

**原因**: ファイルが他のプロセスで使用中

**対処法**:
- SyncBridge.exeやエクスプローラーを閉じる
- PowerShellを管理者として実行

### 問題: マーカーファイルのタイムスタンプが正しくない

**原因**: システムの時刻設定やタイムゾーンの問題

**対処法**:
- マーカーファイルを削除して再展開
  ```powershell
  Remove-Item "$env:TEMP\SyncBridgeTest\runtime\dotnet-test\.zip-extracted"
  ```

---

## デバッグ情報

### マーカーファイルの内容

マーカーファイル（`.zip-extracted`）には以下の情報が含まれます:

```
ZipFileName=dotnet-test.zip
ZipTimestampUtc=2026-02-05T10:30:00Z
ExtractedAtUtc=2026-02-05T10:30:15Z
```

### コンソール出力の例

```
[情報] SyncBridge 開始
[情報] 同期開始: C:\TestData\SyncBridge -> C:\Users\...\AppData\Local\Temp\SyncBridgeTest
[情報] ZIP同期戦略: InitialExtraction - マーカーファイルが存在しないため、初回展開を実行します
[情報] ZIPファイルを展開しています: C:\TestData\SyncBridge\dotnet-test.zip
[情報] 3 個のファイルを展開しました
[情報] ランタイム同期: 3ファイル更新
[情報] ZIP同期戦略: InitialExtraction - マーカーファイルが存在しないため、初回展開を実行します
[情報] ZIPファイルを展開しています: C:\TestData\SyncBridge\TestApp.zip
[情報] 3 個のファイルを展開しました
[情報] TestApp同期: 3ファイル更新
[情報] 同期完了: 合計6ファイル
[情報] SyncBridge 終了
```

---

## クリーンアップ

テスト後にデータをクリーンアップするには:

```powershell
# テストデータ削除
Remove-Item -Path "C:\TestData\SyncBridge" -Recurse -Force

# ローカル同期先削除
Remove-Item -Path "$env:TEMP\SyncBridgeTest" -Recurse -Force
```
