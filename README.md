# Aloe.Apps.SyncBridge

.NETアプリケーション配布のためのクライアントサイド同期・起動ツール

## 概要

SyncBridgeは、ネットワーク上の共有フォルダからアプリケーションとランタイムを同期し、起動するブートストラッパーです。ClickOnceで配布可能なConsoleアプリケーションとして実装されています。

### 主な機能

- **ファイル同期**: ネットワーク上のファイルをローカルに同期
  - ファイル日時比較によるバージョン管理
  - ロールバック対応
- **アプリケーション起動**: 同期後に指定されたアプリケーションを自動起動
- **ランタイム管理**: .NET Desktop Runtimeを含むランタイムの一元管理
- **マニフェスト制御**: INI設定ファイルによるバージョン・起動設定の管理（Win32 API使用、外部依存なし）

## システム要件

- .NET Framework 4.6.1 以降
- Windows OS
- ネットワーク共有フォルダへのアクセス権

## アーキテクチャ

### フォルダ構成

同期されるファイルは `%LocalAppData%\Company\SyncBridge\` 以下に展開されます：

```
%LocalAppData%\Company\SyncBridge\
├── runtime/
│   └── dotnet-10.0.2/          # .NET Desktop Runtime一式
├── apps/
│   ├── AppA-1.4.7/             # アプリケーションA
│   └── AppB-2.1.0/             # アプリケーションB
└── manifest.ini                # 起動設定・バージョン管理
```

### 起動フロー

1. SyncBridgeがClickOnce/exeとして起動
2. manifest.iniを読み込み（Win32 API使用）
3. ソースフォルダ（ネットワーク共有）からローカルに同期
4. 指定されたアプリケーションを起動
   - `dotnet.exe`のフルパスを指定して起動
   - 環境変数 `DOTNET_ROOT_X64` を設定（オプション）
   - `UseShellExecute = false` で直接起動

## 設定方法

### manifest.ini

同期とアプリケーション起動の設定は `manifest.ini` で管理します（Windows INI形式）。

```ini
[Manifest]
Version=1.0
SourceRootPath=\\shared-server\DeployRoot
LocalBasePath=%LocalAppData%\Company\SyncBridge

[Runtime]
Version=10.0.2
RelativePath=runtime/dotnet-10.0.2

[SyncOptions]
RetryCount=3
TimeoutSeconds=300
SkipPatterns=*.pdb;*.log

[App.AppA]
DisplayName=Application A
Version=1.4.7
RelativePath=apps/AppA-1.4.7
EntryDll=AppA.dll
LaunchArgPattern=--app=AppA
```

**特徴**:
- Win32 API (`GetPrivateProfileString`) で解析
- 外部ライブラリ依存なし（NuGetパッケージ不要）
- 環境変数の自動展開対応

### 設定項目

- **sourceRootPath**: 同期元のネットワークパス
- **localBasePath**: ローカルの展開先パス
- **runtime**: .NET Runtimeのバージョンとパス
- **applications**: 起動可能なアプリケーションのリスト
- **syncOptions**: 同期オプション（除外パターン、リトライ回数など）

## 使用方法

### 基本的な使用

```bash
# SyncBridge起動（アプリケーション選択または引数で指定）
SyncBridge.exe

# 引数で起動アプリを指定
SyncBridge.exe --app=AppA
```

引数は指定されたアプリケーションにそのまま渡されます。

### プロトコル・拡張子関連付け（将来対応予定）

特定のプロトコルやファイル拡張子からの起動にも対応予定です。

## プロジェクト構造

```
aloe-apps-syncbridge/
├── doc/
│   └── manifest.json.sample        # マニフェストファイルサンプル
├── src/
│   ├── Aloe.Apps.SyncBridge.slnx   # ソリューションファイル
│   └── Aloe/Apps/SyncBridge/
│       ├── Aloe.Apps.SyncBridge/           # Consoleアプリ本体
│       │   ├── Program.cs
│       │   └── App.config
│       ├── Aloe.Apps.DummyWpf/             # サンプルWPFアプリケーション
│       │   ├── MainWindow.xaml
│       │   ├── MainWindow.xaml.cs
│       │   └── App.xaml
│       └── Aloe.Apps.SyncBridgeLib/        # コアライブラリ
│           ├── Models/
│           │   └── SyncManifest.cs         # マニフェストモデル
│           ├── Repositories/
│           │   ├── IManifestRepository.cs
│           │   └── IniManifestRepository.cs
│           └── Services/
│               ├── IAppLauncher.cs
│               ├── IAppSelector.cs
│               ├── IFileSynchronizer.cs
│               ├── ISyncOrchestrator.cs
│               ├── AppLauncher.cs
│               ├── AppSelector.cs
│               ├── FileSynchronizer.cs
│               ├── SyncOrchestrator.cs
│               └── SyncBridgeBootstrapper.cs
└── test/                               # テストプロジェクト
    ├── manifests/                      # テスト用マニフェスト
    │   ├── dummywpf.ini               # DummyWpfテスト用
    │   ├── minimal.ini
    │   ├── standard.ini
    │   └── ...
    ├── setup-test-data.ps1             # テストデータセットアップ
    └── setup-manifest.ps1              # マニフェスト配置スクリプト
```

## サンプルアプリケーション

### DummyWpf

実際のアプリケーション起動をテストするためのWPFサンプルアプリケーションが含まれています。

**機能:**
- アプリケーション情報表示（アセンブリ名、バージョン、作業ディレクトリ）
- コマンドライン引数の表示
- 環境変数の表示

**使用方法:**

1. DummyWpfをビルド:
```bash
dotnet build src/Aloe/Apps/SyncBridge/Aloe.Apps.DummyWpf
```

2. ビルド成果物をテストデータディレクトリに配置:
```powershell
# 配置先ディレクトリ作成
$sourceRoot = "C:\TestData\Source"
$dummyWpfDir = Join-Path $sourceRoot "apps\DummyWpf-1.0.0"
New-Item -ItemType Directory -Path $dummyWpfDir -Force

# ビルド成果物をコピー
$publishDir = "src\Aloe\Apps\SyncBridge\Aloe.Apps.DummyWpf\bin\Debug\net10.0-windows"
Copy-Item "$publishDir\*" -Destination $dummyWpfDir -Recurse -Force
```

3. テスト用マニフェストを配置:
```powershell
.\test\setup-manifest.ps1
```

4. SyncBridgeを実行してDummyWpfを起動:
```bash
.\src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\bin\Debug\SyncBridge.exe
```

DummyWpfが起動し、SyncBridgeから渡されたコマンドライン引数と環境変数が表示されます。

## ビルド方法

```bash
# クリーン
src/_clean.cmd

# ビルドと発行
src/_publish.cmd
```

## ライセンス

TBD

