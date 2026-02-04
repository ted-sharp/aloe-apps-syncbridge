# SyncBridge テストデータセットアップスクリプト

param(
    [string]$TestDataRoot = "C:\TestData\Source",
    [switch]$Clean,
    [switch]$BuildDummyWpf
)

Write-Host "=== SyncBridge Test Data Setup ===" -ForegroundColor Cyan

# クリーンアップ
if ($Clean -and (Test-Path $TestDataRoot)) {
    Write-Host "Cleaning existing test data at: $TestDataRoot" -ForegroundColor Yellow
    Remove-Item -Path $TestDataRoot -Recurse -Force
}

# テストデータディレクトリの作成
Write-Host "`nCreating test data directories..." -ForegroundColor Green

$directories = @(
    "$TestDataRoot\runtime\dotnet-8.0.0",
    "$TestDataRoot\runtime\dotnet-10.0.0-preview.1",
    "$TestDataRoot\runtime\dotnet-10.0.0",
    "$TestDataRoot\apps\TestApp-1.0.0",
    "$TestDataRoot\apps\App1-1.2.3",
    "$TestDataRoot\apps\App2-2.0.0",
    "$TestDataRoot\apps\App3-0.9.5",
    "$TestDataRoot\apps\NetworkApp-1.0.0",
    "$TestDataRoot\apps\App_With_Underscores-1.0.0",
    "$TestDataRoot\apps\DummyWpf-1.0.0"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
        Write-Host "  Created: $dir" -ForegroundColor Gray
    }
}

# ダミーDLLファイルの作成
Write-Host "`nCreating dummy DLL files..." -ForegroundColor Green

$dummyFiles = @(
    "$TestDataRoot\apps\TestApp-1.0.0\TestApp.dll",
    "$TestDataRoot\apps\App1-1.2.3\App1.dll",
    "$TestDataRoot\apps\App2-2.0.0\App2.Main.dll",
    "$TestDataRoot\apps\App3-0.9.5\App3.exe",
    "$TestDataRoot\apps\NetworkApp-1.0.0\NetworkApp.dll",
    "$TestDataRoot\apps\App_With_Underscores-1.0.0\App.Main.dll"
)

foreach ($file in $dummyFiles) {
    "Dummy DLL for testing" | Out-File -FilePath $file -Encoding UTF8
    Write-Host "  Created: $file" -ForegroundColor Gray
}

# スキップパターンテスト用ファイルの作成
Write-Host "`nCreating skip pattern test files..." -ForegroundColor Green

$skipTestFiles = @(
    "$TestDataRoot\apps\App1-1.2.3\App1.pdb",
    "$TestDataRoot\apps\App1-1.2.3\debug.log",
    "$TestDataRoot\apps\App2-2.0.0\test-data.tmp",
    "$TestDataRoot\apps\App2-2.0.0\temp\cache.dat"
)

foreach ($file in $skipTestFiles) {
    $dir = Split-Path $file -Parent
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    "Skip pattern test file" | Out-File -FilePath $file -Encoding UTF8
    Write-Host "  Created: $file" -ForegroundColor Gray
}

# Runtime ダミーファイル
Write-Host "`nCreating runtime files..." -ForegroundColor Green

$runtimeFiles = @(
    "$TestDataRoot\runtime\dotnet-8.0.0\dotnet.exe",
    "$TestDataRoot\runtime\dotnet-10.0.0-preview.1\dotnet.exe",
    "$TestDataRoot\runtime\dotnet-10.0.0\dotnet.exe"
)

foreach ($file in $runtimeFiles) {
    "Dummy runtime" | Out-File -FilePath $file -Encoding UTF8
    Write-Host "  Created: $file" -ForegroundColor Gray
}

# 設定ファイルの作成
Write-Host "`nCreating configuration files..." -ForegroundColor Green

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="TestMode" value="true" />
  </appSettings>
</configuration>
"@ | Out-File -FilePath "$TestDataRoot\apps\App1-1.2.3\App.config" -Encoding UTF8

@"
{
  "AppSettings": {
    "Environment": "Test",
    "Version": "1.2.3"
  }
}
"@ | Out-File -FilePath "$TestDataRoot\apps\App1-1.2.3\appsettings.json" -Encoding UTF8

# DummyWpf のビルドと配置
if ($BuildDummyWpf) {
    Write-Host "`nBuilding and deploying DummyWpf..." -ForegroundColor Green

    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectPath = Join-Path $scriptDir "..\src\Aloe\Apps\SyncBridge\Aloe.Apps.DummyWpf\Aloe.Apps.DummyWpf.csproj"

    if (Test-Path $projectPath) {
        Write-Host "  Building DummyWpf project..." -ForegroundColor Gray

        # プロジェクトをビルド
        $buildOutput = dotnet build $projectPath --configuration Release 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host "  Build successful!" -ForegroundColor Green

            # ビルド成果物の配置
            $publishDir = Join-Path (Split-Path $projectPath -Parent) "bin\Release\net10.0-windows"
            $targetDir = "$TestDataRoot\apps\DummyWpf-1.0.0"

            if (Test-Path $publishDir) {
                Write-Host "  Copying DummyWpf binaries to $targetDir" -ForegroundColor Gray
                Copy-Item "$publishDir\*" -Destination $targetDir -Recurse -Force
                Write-Host "  DummyWpf deployed successfully!" -ForegroundColor Green
            } else {
                Write-Host "  Warning: Build output directory not found: $publishDir" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  Build failed. Using dummy DLL instead." -ForegroundColor Yellow
            "Dummy DummyWpf.dll for testing" | Out-File -FilePath "$TestDataRoot\apps\DummyWpf-1.0.0\DummyWpf.dll" -Encoding UTF8
        }
    } else {
        Write-Host "  Warning: DummyWpf project not found at: $projectPath" -ForegroundColor Yellow
        Write-Host "  Creating dummy DLL instead." -ForegroundColor Gray
        "Dummy DummyWpf.dll for testing" | Out-File -FilePath "$TestDataRoot\apps\DummyWpf-1.0.0\DummyWpf.dll" -Encoding UTF8
    }
} else {
    # ダミーDLLのみ作成
    Write-Host "`nCreating dummy DummyWpf.dll (use -BuildDummyWpf to build actual application)..." -ForegroundColor Green
    "Dummy DummyWpf.dll for testing" | Out-File -FilePath "$TestDataRoot\apps\DummyWpf-1.0.0\DummyWpf.dll" -Encoding UTF8
    Write-Host "  Created: $TestDataRoot\apps\DummyWpf-1.0.0\DummyWpf.dll" -ForegroundColor Gray
}

Write-Host "`n=== Setup Complete ===" -ForegroundColor Cyan
Write-Host "Test data root: $TestDataRoot" -ForegroundColor White
Write-Host "`nYou can now use the test manifests in the test/manifests directory." -ForegroundColor White
if (-not $BuildDummyWpf) {
    Write-Host "`nTip: Use -BuildDummyWpf to build and deploy the actual DummyWpf application." -ForegroundColor Yellow
}
