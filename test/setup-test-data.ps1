# SyncBridge テストデータセットアップスクリプト

param(
    [string]$TestDataRoot = "C:\TestData\Source",
    [switch]$Clean,
    [switch]$BuildDummyWpf,
    [switch]$DownloadRealRuntime,
    [switch]$UseRuntimeCache = $true,
    [string]$RuntimeCacheDir = "$env:TEMP\SyncBridge-RuntimeCache"
)

# ========================================
# Helper Functions for Runtime Download
# ========================================

function Get-LatestDotNetVersion {
    param(
        [string]$MajorVersion = "10.0"
    )

    try {
        Write-Host "  Querying latest .NET $MajorVersion version..." -ForegroundColor Gray

        # Enable TLS 1.2
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

        $metadataUrl = "https://dotnetcli.azureedge.net/dotnet/release-metadata/$MajorVersion/releases.json"
        $response = Invoke-WebRequest -Uri $metadataUrl -UseBasicParsing -TimeoutSec 30
        $releases = $response.Content | ConvertFrom-Json

        Write-Host "  Metadata retrieved successfully. Found $($releases.releases.Count) releases." -ForegroundColor Gray

        # Get latest stable release
        $latestRelease = $releases.releases |
            Where-Object { $_.release -notlike "*preview*" -and $_.release -notlike "*rc*" } |
            Select-Object -First 1

        if (-not $latestRelease) {
            throw "No stable release found in metadata"
        }

        $version = $latestRelease."release-version"
        Write-Host "  Latest stable version: $version" -ForegroundColor Gray

        # Find Base Runtime x64 download
        $baseRuntime = $latestRelease.runtime.files |
            Where-Object {
                $_.rid -eq "win-x64" -and
                $_.name -like "dotnet-runtime-win-x64.zip"
            }

        if (-not $baseRuntime) {
            Write-Host "  Available files in runtime section:" -ForegroundColor Yellow
            $latestRelease.runtime.files | ForEach-Object {
                Write-Host "    - name: $($_.name), rid: $($_.rid)" -ForegroundColor DarkGray
            }
            throw "Base Runtime x64 ZIP not found in metadata"
        }

        # Find Windows Desktop Runtime x64 download
        $windowsDesktopRuntime = $latestRelease.windowsdesktop.files |
            Where-Object {
                $_.rid -eq "win-x64" -and
                $_.name -like "windowsdesktop-runtime-win-x64.zip"
            }

        if (-not $windowsDesktopRuntime) {
            Write-Host "  Available files in windowsdesktop section:" -ForegroundColor Yellow
            $latestRelease.windowsdesktop.files | ForEach-Object {
                Write-Host "    - name: $($_.name), rid: $($_.rid)" -ForegroundColor DarkGray
            }
            throw "Windows Desktop Runtime x64 ZIP not found in metadata"
        }

        Write-Host "  Found base runtime URL: $($baseRuntime.url)" -ForegroundColor Gray
        Write-Host "  Found desktop runtime URL: $($windowsDesktopRuntime.url)" -ForegroundColor Gray

        return @{
            Version = $version
            BaseRuntime = @{
                DownloadUrl = $baseRuntime.url
                Hash = $baseRuntime.hash
            }
            WindowsDesktop = @{
                DownloadUrl = $windowsDesktopRuntime.url
                Hash = $windowsDesktopRuntime.hash
            }
        }
    }
    catch {
        Write-Host "  Warning: Failed to query metadata: $_" -ForegroundColor Yellow
        Write-Host "  Using fallback URLs..." -ForegroundColor Yellow

        # Fallback to known URL patterns
        return @{
            Version = "10.0.2"
            BaseRuntime = @{
                DownloadUrl = "https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0.2/dotnet-runtime-10.0.2-win-x64.zip"
                Hash = $null
            }
            WindowsDesktop = @{
                DownloadUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/10.0.2/windowsdesktop-runtime-10.0.2-win-x64.zip"
                Hash = $null
            }
        }
    }
}

function Download-DotNetPackage {
    param(
        [string]$Version,
        [string]$DownloadUrl,
        [string]$CacheDir,
        [bool]$UseCache,
        [string]$ExpectedHash = $null,
        [ValidateSet("runtime", "windowsdesktop")]
        [string]$PackageType = "runtime"
    )

    $fileName = if ($PackageType -eq "runtime") {
        "dotnet-runtime-$Version-win-x64.zip"
    } else {
        "windowsdesktop-runtime-$Version-win-x64.zip"
    }
    $cachePath = Join-Path $CacheDir $fileName

    # Check cache
    if ($UseCache -and (Test-Path $cachePath)) {
        Write-Host "  Found cached runtime: $cachePath" -ForegroundColor Gray

        # Verify hash if available and Get-FileHash cmdlet exists
        if ($ExpectedHash -and (Get-Command Get-FileHash -ErrorAction SilentlyContinue)) {
            Write-Host "  Verifying cached file hash..." -ForegroundColor Gray
            $fileHash = (Get-FileHash -Path $cachePath -Algorithm SHA512).Hash

            if ($fileHash -eq $ExpectedHash) {
                Write-Host "  Hash verified! Using cached file." -ForegroundColor Green
                return $cachePath
            }
            else {
                Write-Host "  Hash mismatch! Re-downloading..." -ForegroundColor Yellow
                Remove-Item -Path $cachePath -Force
            }
        }
        else {
            Write-Host "  Using cached file (hash verification skipped)." -ForegroundColor Gray
            return $cachePath
        }
    }

    # Create cache directory
    if (-not (Test-Path $CacheDir)) {
        New-Item -ItemType Directory -Path $CacheDir -Force | Out-Null
    }

    # Download with retry logic
    $maxAttempts = 3
    $attempt = 0

    while ($attempt -lt $maxAttempts) {
        $attempt++

        try {
            $packageName = if ($PackageType -eq "runtime") { "Base Runtime" } else { "Desktop Runtime" }
            Write-Host "  Downloading .NET $Version $packageName (attempt $attempt/$maxAttempts)..." -ForegroundColor Gray
            Write-Host "  URL: $DownloadUrl" -ForegroundColor DarkGray

            # Enable TLS 1.2
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

            # Download with progress
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -Uri $DownloadUrl -OutFile $cachePath -UseBasicParsing -TimeoutSec 300
            $ProgressPreference = 'Continue'

            # Verify download
            if (Test-Path $cachePath) {
                $fileSize = (Get-Item $cachePath).Length / 1MB
                Write-Host "  Downloaded successfully! Size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor Green

                # Verify hash if available and Get-FileHash cmdlet exists
                if ($ExpectedHash -and (Get-Command Get-FileHash -ErrorAction SilentlyContinue)) {
                    Write-Host "  Verifying file hash..." -ForegroundColor Gray
                    $fileHash = (Get-FileHash -Path $cachePath -Algorithm SHA512).Hash

                    if ($fileHash -eq $ExpectedHash) {
                        Write-Host "  Hash verified!" -ForegroundColor Green
                        return $cachePath
                    }
                    else {
                        Write-Host "  Hash mismatch! Expected: $ExpectedHash" -ForegroundColor Red
                        Write-Host "  Got: $fileHash" -ForegroundColor Red
                        Remove-Item -Path $cachePath -Force
                        throw "Hash verification failed"
                    }
                }
                elseif ($ExpectedHash) {
                    Write-Host "  Hash verification skipped (Get-FileHash cmdlet not available)." -ForegroundColor Yellow
                }

                return $cachePath
            }
            else {
                throw "Download completed but file not found"
            }
        }
        catch {
            Write-Host "  Download attempt $attempt failed: $_" -ForegroundColor Yellow

            if ($attempt -ge $maxAttempts) {
                throw "Failed to download after $maxAttempts attempts: $_"
            }

            Start-Sleep -Seconds 2
        }
    }
}

function Extract-DotNetRuntime {
    param(
        [string]$InstallerPath,
        [string]$DestinationPath,
        [switch]$Merge
    )

    try {
        Write-Host "  Extracting runtime to: $DestinationPath" -ForegroundColor Gray

        # Clean destination if exists (only when not merging)
        if (-not $Merge -and (Test-Path $DestinationPath)) {
            Remove-Item -Path $DestinationPath -Recurse -Force
        }

        # Create destination directory
        New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

        # Extract ZIP
        Expand-Archive -Path $InstallerPath -DestinationPath $DestinationPath -Force

        # Verify critical files based on whether we're merging
        if ($Merge) {
            # When merging, verify complete runtime structure
            $criticalFiles = @(
                "dotnet.exe",
                "shared\Microsoft.NETCore.App",
                "shared\Microsoft.WindowsDesktop.App"
            )
        }
        else {
            # When first extracting, only verify base runtime
            $criticalFiles = @(
                "dotnet.exe",
                "shared\Microsoft.NETCore.App"
            )
        }

        foreach ($file in $criticalFiles) {
            $fullPath = Join-Path $DestinationPath $file
            if (-not (Test-Path $fullPath)) {
                throw "Critical file/directory not found after extraction: $file"
            }
        }

        Write-Host "  Extraction successful!" -ForegroundColor Green

        # Display extracted framework versions
        $coreAppPath = Join-Path $DestinationPath "shared\Microsoft.NETCore.App"
        if (Test-Path $coreAppPath) {
            $coreVersions = Get-ChildItem -Path $coreAppPath -Directory | Select-Object -ExpandProperty Name
            Write-Host "  NETCore.App versions: $($coreVersions -join ', ')" -ForegroundColor Gray
        }

        $desktopPath = Join-Path $DestinationPath "shared\Microsoft.WindowsDesktop.App"
        if (Test-Path $desktopPath) {
            $desktopVersions = Get-ChildItem -Path $desktopPath -Directory | Select-Object -ExpandProperty Name
            Write-Host "  WindowsDesktop.App versions: $($desktopVersions -join ', ')" -ForegroundColor Gray
        }

        return $true
    }
    catch {
        Write-Host "  Extraction failed: $_" -ForegroundColor Red

        # Clean up partial extraction (only if not merging)
        if (-not $Merge -and (Test-Path $DestinationPath)) {
            try {
                Remove-Item -Path $DestinationPath -Recurse -Force
            }
            catch {
                Write-Host "  Warning: Could not clean up partial extraction: $_" -ForegroundColor Yellow
            }
        }

        throw
    }
}

# ========================================
# Main Script
# ========================================

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

# Real .NET 10 Runtime Download
if ($DownloadRealRuntime) {
    Write-Host "`nDownloading real .NET 10 Runtime (base + desktop)..." -ForegroundColor Green

    try {
        # 1. Get version info for BOTH packages
        $runtimeInfo = Get-LatestDotNetVersion -MajorVersion "10.0"
        Write-Host "  Latest version: $($runtimeInfo.Version)" -ForegroundColor Gray

        # 2. Download base runtime
        Write-Host "  Downloading base runtime..." -ForegroundColor Gray
        $baseInstallerPath = Download-DotNetPackage `
            -Version $runtimeInfo.Version `
            -DownloadUrl $runtimeInfo.BaseRuntime.DownloadUrl `
            -CacheDir $RuntimeCacheDir `
            -UseCache $UseRuntimeCache `
            -ExpectedHash $runtimeInfo.BaseRuntime.Hash `
            -PackageType "runtime"

        # 3. Download desktop runtime
        Write-Host "  Downloading desktop runtime..." -ForegroundColor Gray
        $desktopInstallerPath = Download-DotNetPackage `
            -Version $runtimeInfo.Version `
            -DownloadUrl $runtimeInfo.WindowsDesktop.DownloadUrl `
            -CacheDir $RuntimeCacheDir `
            -UseCache $UseRuntimeCache `
            -ExpectedHash $runtimeInfo.WindowsDesktop.Hash `
            -PackageType "windowsdesktop"

        # 4. Extract base runtime first
        $targetPath = "$TestDataRoot\runtime\dotnet-10.0.0"
        Write-Host "  Extracting base runtime..." -ForegroundColor Gray
        Extract-DotNetRuntime -InstallerPath $baseInstallerPath -DestinationPath $targetPath

        # 5. Extract desktop runtime (merge into same directory)
        Write-Host "  Extracting desktop runtime..." -ForegroundColor Gray
        Extract-DotNetRuntime -InstallerPath $desktopInstallerPath -DestinationPath $targetPath -Merge

        # 6. Validate complete structure
        $dotnetExePath = Join-Path $targetPath "dotnet.exe"
        if (-not (Test-Path $dotnetExePath)) {
            throw "dotnet.exe not found after extraction!"
        }

        Write-Host "  .NET 10 Runtime (complete) provisioned successfully!" -ForegroundColor Green
    }
    catch {
        Write-Host "  Error downloading runtime: $_" -ForegroundColor Red
        Write-Host "  Falling back to dummy runtime files." -ForegroundColor Yellow
        # Dummy files already created above, so script continues
    }
}
else {
    Write-Host "`nTip: Use -DownloadRealRuntime to download actual .NET 10 runtime for testing." -ForegroundColor Yellow
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

$tips = @()
if (-not $BuildDummyWpf) {
    $tips += "Use -BuildDummyWpf to build and deploy the actual DummyWpf application"
}
if (-not $DownloadRealRuntime) {
    $tips += "Use -DownloadRealRuntime to download actual .NET 10 runtime for testing"
}

if ($tips.Count -gt 0) {
    Write-Host "`nTips:" -ForegroundColor Yellow
    foreach ($tip in $tips) {
        Write-Host "  - $tip" -ForegroundColor Yellow
    }
}
