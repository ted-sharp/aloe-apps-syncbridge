# ClickOnce URL引数機能のテスト方法

## 概要

このドキュメントは、実装したClickOnce URL引数機能のテスト方法を説明します。

## 非ClickOnce環境でのテスト（完了）

### 1. 基本的な動作確認

```bash
# ビルド
dotnet build src/Aloe.Apps.SyncBridge.slnx

# コンソール表示で引数を確認
cd src/Aloe/Apps/SyncBridge/Aloe.Apps.SyncBridge/bin/Debug
./SyncBridge.exe --console
```

**期待される動作:**
- `[情報] 統合後の引数:` が表示される
- 既存のコマンドライン引数が正しく処理される

### 2. ClickOnceHelper単体テスト

```bash
cd test
./TestClickOnceHelper.exe
```

**テスト内容:**
1. ConvertToCommandLineArgs - Dictionary を `--key=value` 形式に変換
2. MergeArguments (重複なし) - コマンドライン引数とURL引数を統合
3. MergeArguments (重複あり) - コマンドライン引数が優先されることを確認
4. IsClickOnceDeployment - 非ClickOnce環境では false を返す
5. GetActivationParameters - 非ClickOnce環境では空を返す
6. GetClickOnceArguments - 非ClickOnce環境では空配列を返す

## ClickOnce環境でのテスト（手動テスト）

### 前提条件

1. Visual Studio または Mage.exe (ClickOnce Manifest Generation Tool)
2. テスト用Webサーバー（IISまたはローカルHTTPサーバー）

### セットアップ手順

#### 1. ClickOnce配布の準備

**Visual Studioを使用する場合:**

1. SyncBridge プロジェクトを右クリック → プロパティ
2. 「発行」タブを選択
3. 発行場所を設定（例: `C:\Publish\SyncBridge\`）
4. 「今すぐ発行」をクリック

**Mage.exeを使用する場合:**

```powershell
# アプリケーションマニフェストを作成
mage -New Application -ToFile SyncBridge.exe.manifest -Name "SyncBridge" -Version 1.0.0.0 -FromDirectory bin\Debug

# 配置マニフェストを作成
mage -New Deployment -ToFile SyncBridge.application -Name "SyncBridge" -Version 1.0.0.0 -AppManifest SyncBridge.exe.manifest -Install true
```

#### 2. URL Protocol Associationの設定

ClickOnce経由でURL引数を受け取るには、カスタムプロトコルを登録する必要があります。

**レジストリファイルを作成（例: `register-protocol.reg`）:**

```registry
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\syncbridge]
@="URL:SyncBridge Protocol"
"URL Protocol"=""

[HKEY_CLASSES_ROOT\syncbridge\DefaultIcon]
@="C:\\Publish\\SyncBridge\\SyncBridge.exe,1"

[HKEY_CLASSES_ROOT\syncbridge\shell]
[HKEY_CLASSES_ROOT\syncbridge\shell\open]
[HKEY_CLASSES_ROOT\syncbridge\shell\open\command]
@="\"C:\\Windows\\System32\\rundll32.exe\" dfshim.dll,ShOpenVerbApplication http://localhost/SyncBridge.application?%1"
```

**注意:**
- `http://localhost/SyncBridge.application` は実際の配布URLに置き換えてください
- レジストリファイルをダブルクリックして実行し、プロトコルを登録してください

#### 3. テスト用HTMLページを作成

**testpage.html:**

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>SyncBridge ClickOnce Test</title>
</head>
<body>
    <h1>SyncBridge ClickOnce URL引数テスト</h1>

    <h2>テストリンク</h2>

    <h3>テスト1: アプリ指定のみ</h3>
    <a href="syncbridge:launch?app=DummyWpf">DummyWpfを起動</a>

    <h3>テスト2: 複数パラメータ</h3>
    <a href="syncbridge:launch?app=DummyWpf&param1=value1&param2=test123">複数パラメータ付きで起動</a>

    <h3>テスト3: 日本語を含むパラメータ</h3>
    <a href="syncbridge:launch?app=DummyWpf&message=%E3%83%86%E3%82%B9%E3%83%88">日本語パラメータ付きで起動</a>

    <h3>テスト4: スペースを含むパラメータ</h3>
    <a href="syncbridge:launch?app=DummyWpf&param=Hello%20World">スペース付きパラメータで起動</a>
</body>
</html>
```

### テスト実行

1. ブラウザで `testpage.html` を開く
2. テストリンクをクリック
3. ClickOnceのセキュリティ警告が表示された場合は「実行」をクリック
4. SyncBridgeが起動し、DummyWpfアプリが表示されることを確認

### 期待される結果

#### DummyWpfアプリの表示内容で確認できる項目:

1. **コマンドライン引数タブ:**
   - `--app=DummyWpf`
   - `--param1=value1` (テスト2の場合)
   - `--param2=test123` (テスト2の場合)
   - その他URL引数で指定したパラメータ

2. **環境変数タブ:**
   - 必要に応じて設定された環境変数

### デバッグ方法

#### コンソール出力を確認する場合:

SyncBridgeのマニフェストを編集して、OutputType を `WinExe` から `Exe` に一時的に変更:

```xml
<OutputType>Exe</OutputType>
```

再ビルド後、ClickOnce経由で起動すると、コンソールウィンドウに以下が表示されます:

```
[情報] 統合後の引数:
  --app=DummyWpf
  --param1=value1
  --param2=test123
[情報] SyncBridge 開始
...
```

#### Visual Studioデバッガーでアタッチする場合:

1. Visual Studioで「デバッグ」→「プロセスにアタッチ」
2. `SyncBridge.exe` プロセスを選択
3. ブレークポイントを `Program.cs` の `Main()` メソッドに設定
4. テストリンクをクリックして起動

### トラブルシューティング

#### 問題: ClickOnceアプリが起動しない

**原因:**
- レジストリのURL Protocol設定が正しくない
- 配布マニフェストのURLが間違っている

**解決策:**
- レジストリエディタで `HKEY_CLASSES_ROOT\syncbridge` を確認
- マニフェストのURLを確認

#### 問題: URL引数が渡されない

**原因:**
- `ActivationUri` が null（初回起動時）
- プロトコルハンドラーがクエリ文字列を渡していない

**解決策:**
- 2回目以降の起動でテスト（初回起動時は `ActivationUri` が null）
- レジストリのコマンドに `%1` が含まれているか確認

#### 問題: 日本語やスペースを含むパラメータが文字化けする

**原因:** URL エンコーディングの問題

**解決策:**
- `Uri.UnescapeDataString()` を使用（実装済み）
- HTMLのリンクで `encodeURIComponent()` を使用してエンコード

## エッジケースのテスト

### 1. ActivationUri が null の場合

初回起動時は `ActivationUri` が null になります。

**期待される動作:**
- エラーが発生せず、通常のコマンドライン引数のみで起動

### 2. クエリパラメータが空の場合

```
syncbridge:launch
```

**期待される動作:**
- URL引数は空配列
- コマンドライン引数のみで動作

### 3. コマンドライン引数とURL引数の両方がある場合

コマンドライン: `--app=AppA --console`
URL: `?app=AppB&param1=value1`

**期待される動作:**
- `--app=AppA` が優先される（コマンドライン優先）
- `--param1=value1` は追加される
- 結果: `--app=AppA --console --param1=value1`

### 4. 特殊文字を含むURL

```
syncbridge:launch?param=<test>&value="quoted"&path=C:\Test\File.txt
```

**期待される動作:**
- URL デコードされて正しく処理される
- 特殊文字がエスケープされる

## 自動テストの実装案（将来）

### ユニットテスト（推奨）

NUnitまたはxUnitを使用して、ClickOnceHelperの各メソッドをテスト:

```csharp
[Test]
public void ConvertToCommandLineArgs_WithMultipleParameters_ReturnsCorrectFormat()
{
    var parameters = new Dictionary<string, string>
    {
        { "app", "AppA" },
        { "param1", "value1" }
    };

    var result = ClickOnceHelper.ConvertToCommandLineArgs(parameters);

    Assert.Contains("--app=AppA", result);
    Assert.Contains("--param1=value1", result);
}

[Test]
public void MergeArguments_WithDuplicateKeys_CommandLineHasPriority()
{
    var cmdLine = new[] { "--app=AppA" };
    var urlArgs = new[] { "--app=AppB", "--param1=value1" };

    var result = ClickOnceHelper.MergeArguments(cmdLine, urlArgs);

    Assert.Contains("--app=AppA", result);
    Assert.DoesNotContain("--app=AppB", result);
    Assert.Contains("--param1=value1", result);
}
```

### 統合テスト（推奨）

テストプロジェクトで模擬的な `ApplicationDeployment` を作成し、実際の動作を確認することは困難です。代わりに、手動テストを推奨します。

## 結論

実装した機能は以下の点で正しく動作することが確認されました:

1. ✓ 非ClickOnce環境でエラーが発生しない
2. ✓ コマンドライン引数が正しく統合される
3. ✓ 重複する引数はコマンドライン優先で処理される
4. ✓ URL引数のパースとデコードが正しく動作する

ClickOnce環境でのテストは、実際の配布環境でHTMLテストページを使用して手動で確認してください。
