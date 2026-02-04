# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SyncBridge is a .NET application distribution tool that synchronizes applications and runtimes from network shares to local machines and launches them. It's implemented as a .NET Framework 4.6.1 Console application designed for ClickOnce deployment.

**Key Architecture Concept**: The tool operates in three phases:
1. Load manifest (JSON configuration from network share or local path)
2. Synchronize files (runtime + applications) from source to `%LocalAppData%\Company\SyncBridge\`
3. Launch selected application using synchronized .NET runtime via `dotnet.exe`

## Build & Run Commands

### Build
```bash
# Build all projects (run from src/ directory)
dotnet build Aloe.Apps.SyncBridge.slnx

# Build main console application
dotnet build src/Aloe/Apps/SyncBridge/Aloe.Apps.SyncBridge

# Build DummyWpf sample app for testing
dotnet build src/Aloe/Apps/SyncBridge/Aloe.Apps.DummyWpf
```

### Run
```bash
# Run from source (from repository root)
dotnet run --project src/Aloe/Apps/SyncBridge/Aloe.Apps.SyncBridge

# Run with specific app selection
dotnet run --project src/Aloe/Apps/SyncBridge/Aloe.Apps.SyncBridge -- --app=AppA
```

### Clean & Publish
```bash
# Clean build artifacts (from src/ directory)
src/_clean.cmd

# Publish (currently references non-existent projects, needs updating)
src/_publish.cmd
```

## Project Structure

### Three Main Projects

1. **Aloe.Apps.SyncBridge** - Console entry point (`Program.cs`)
   - Initializes dependency instances (manual DI)
   - Calls `SyncBridgeBootstrapper.Execute(args)`

2. **Aloe.Apps.SyncBridgeLib** - Core library with layers:
   - **Models/** - `SyncManifest`, `AppConfig`, `RuntimeConfig`, `SyncOptions`
   - **Repositories/** - `JsonManifestRepository` (loads manifest.json, expands env vars)
   - **Services/** - Core orchestration logic:
     - `SyncBridgeBootstrapper` - Main execution flow coordinator
     - `SyncOrchestrator` - Coordinates file synchronization
     - `FileSynchronizer` - File copying with date comparison
     - `AppSelector` - Selects target app from args or user prompt
     - `AppLauncher` - Launches app via `Process.Start()` with dotnet.exe

3. **Aloe.Apps.DummyWpf** - Sample WPF app for testing
   - Displays command-line args, environment variables, assembly info

### Dependency Flow
```
Program.cs
  → SyncBridgeBootstrapper
    → ManifestRepository (load JSON)
    → SyncOrchestrator → FileSynchronizer
    → AppSelector (choose app)
    → AppLauncher (start process)
```

## Manifest System

Configuration is driven by `manifest.json` which specifies:
- Source network path (`sourceRootPath`)
- Local sync destination (`localBasePath`)
- Runtime version and path
- Application list with versions, paths, entry DLLs
- Sync options (skip patterns, retry, timeout)

**Manifest locations searched (in order)**:
1. `%LocalAppData%\Company\SyncBridge\manifest.json`
2. Same directory as SyncBridge.exe

Test manifests are in `test/manifests/`:
- `minimal.json` - Single app, basic configuration
- `standard.json` - Multiple apps with skip patterns
- `dummywpf.json` - Configuration for DummyWpf sample
- `empty-apps.json` - Error handling test (no apps)
- `network-share.json` - UNC path testing

## Testing

### Setup Test Environment
```powershell
# Create test data structure
.\test\setup-test-data.ps1

# Build and deploy DummyWpf for integration testing
.\test\setup-test-data.ps1 -BuildDummyWpf

# Clean and recreate test data
.\test\setup-test-data.ps1 -Clean

# Custom test data location
.\test\setup-test-data.ps1 -TestDataRoot "D:\MyTestData"
```

### Manual Integration Test
```powershell
# 1. Setup test manifest
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$targetDir = Join-Path $localAppData "Company\SyncBridge"
New-Item -ItemType Directory -Path $targetDir -Force
Copy-Item "test\manifests\dummywpf.json" -Destination (Join-Path $targetDir "manifest.json")

# 2. Run SyncBridge (launches DummyWpf)
dotnet run --project src/Aloe/Apps/SyncBridge/Aloe.Apps.SyncBridge
```

See `test/README.md` for detailed test scenarios.

## Code Conventions

- Uses .NET Framework 4.6.1 (old-style .csproj format)
- Console messages are in Japanese
- Interface-based architecture (all services have I-prefixed interfaces)
- Manual dependency injection in `Program.cs`
- File sync uses timestamp comparison (not hash-based)
- Environment variables in manifest paths are auto-expanded by `JsonManifestRepository`

## Important Implementation Details

- **Process Launch**: Uses `UseShellExecute = false` to directly invoke `dotnet.exe` with the application DLL path
- **Environment Variables**: Can set `DOTNET_ROOT_X64` via `LaunchContext`
- **File Sync**: Compares `LastWriteTime`, copies if source is newer
- **Rollback**: Not yet implemented (mentioned as future feature in README)
- **No logging framework**: Currently uses `Console.WriteLine()` with Japanese prefixes like `[情報]`, `[エラー]`
