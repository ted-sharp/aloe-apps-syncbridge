# ZIP機能テスト用データセットアップスクリプト

param(
    [string]$TestDataRoot = "C:\TestData\SyncBridge",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

Write-Host "=== SyncBridge ZIP機能テストデータセットアップ ===" -ForegroundColor Cyan

# クリーンアップ
if ($Clean -and (Test-Path $TestDataRoot)) {
    Write-Host "既存のテストデータを削除しています..." -ForegroundColor Yellow
    Remove-Item -Path $TestDataRoot -Recurse -Force
}

# ディレクトリ作成
Write-Host "テストディレクトリを作成しています..." -ForegroundColor Green
$runtimeDir = Join-Path $TestDataRoot "runtime\dotnet-test"
$appDir = Join-Path $TestDataRoot "apps\TestApp"

New-Item -ItemType Directory -Path $runtimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $appDir -Force | Out-Null

# ダミーファイル作成
Write-Host "ダミーファイルを作成しています..." -ForegroundColor Green

# Runtime用ファイル
Set-Content -Path (Join-Path $runtimeDir "dotnet.exe") -Value "dummy runtime executable" -Encoding ASCII
Set-Content -Path (Join-Path $runtimeDir "runtime.dll") -Value "dummy runtime library" -Encoding ASCII
Set-Content -Path (Join-Path $runtimeDir "README.txt") -Value "This is a test runtime for SyncBridge" -Encoding ASCII

# App用ファイル
Set-Content -Path (Join-Path $appDir "TestApp.dll") -Value "dummy application assembly" -Encoding ASCII
Set-Content -Path (Join-Path $appDir "TestApp.deps.json") -Value '{"runtimeTarget":{"name":".NETCoreApp,Version=v8.0"}}' -Encoding ASCII
Set-Content -Path (Join-Path $appDir "config.json") -Value '{"setting1": "value1", "setting2": "value2"}' -Encoding ASCII

# ZIP作成
Write-Host "ZIPファイルを作成しています..." -ForegroundColor Green

$runtimeZip = Join-Path $TestDataRoot "dotnet-test.zip"
$appZip = Join-Path $TestDataRoot "TestApp.zip"

# 既存のZIPを削除
if (Test-Path $runtimeZip) { Remove-Item $runtimeZip -Force }
if (Test-Path $appZip) { Remove-Item $appZip -Force }

# Runtime ZIP作成
Compress-Archive -Path (Join-Path $runtimeDir "*") -DestinationPath $runtimeZip -CompressionLevel Optimal

# App ZIP作成
Compress-Archive -Path (Join-Path $appDir "*") -DestinationPath $appZip -CompressionLevel Optimal

Write-Host ""
Write-Host "=== セットアップ完了 ===" -ForegroundColor Green
Write-Host "テストデータルート: $TestDataRoot" -ForegroundColor Cyan
Write-Host ""
Write-Host "作成されたファイル:" -ForegroundColor Yellow
Write-Host "  - $runtimeZip" -ForegroundColor White
Write-Host "  - $appZip" -ForegroundColor White
Write-Host "  - $runtimeDir\" -ForegroundColor White
Write-Host "  - $appDir\" -ForegroundColor White
Write-Host ""
Write-Host "次のステップ:" -ForegroundColor Yellow
Write-Host "  1. test\manifests\zip-test.ini を SyncBridge.exe と同じディレクトリに manifest.ini としてコピー" -ForegroundColor White
Write-Host "  2. SyncBridge.exe を実行してテスト" -ForegroundColor White
Write-Host ""
