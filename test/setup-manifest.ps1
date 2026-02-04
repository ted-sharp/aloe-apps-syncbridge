$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$targetDir = Join-Path $localAppData 'Company\SyncBridge'
New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
Copy-Item 'test\manifests\minimal.ini' -Destination (Join-Path $targetDir 'manifest.ini') -Force
Write-Host "マニフェストをコピーしました: $(Join-Path $targetDir 'manifest.ini')"
