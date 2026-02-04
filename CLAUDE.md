# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

SyncBridge is a .NET application distribution tool that synchronizes applications and runtimes from network shares to local machines and launches them. It's implemented as a .NET Framework 4.6.1 Console application designed for ClickOnce deployment.

**Key Architecture Concept**: The tool operates in three phases:
1. Load manifest (INI configuration from network share or local path)
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
   - **Repositories/** - `IniManifestRepository` (loads manifest.ini using Win32 API, expands env vars)
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
    → ManifestRepository (load INI)
    → SyncOrchestrator → FileSynchronizer
    → AppSelector (choose app)
    → AppLauncher (start process)
```

## Manifest System

Configuration is driven by `manifest.ini` which specifies:
- Source network path (`sourceRootPath`)
- Local sync destination (`localBasePath`)
- Runtime version and path
- Application list with versions, paths, entry DLLs
- Sync options (skip patterns, retry, timeout)

**Manifest location**:
- Same directory as SyncBridge.exe
- A default `manifest.ini` is included in the project and copied to the output directory during build

**Manifest Format**: Uses Windows INI format, parsed via Win32 API (GetPrivateProfileString) for zero external dependencies.

Test manifests are in `test/manifests/`:
- `minimal.ini` - Single app, basic configuration
- `standard.ini` - Multiple apps with skip patterns
- `dummywpf.ini` - Configuration for DummyWpf sample

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
.\test\setup-manifest.ps1

# 2. Run SyncBridge directly (.NET Framework 4.6.1 console app)
.\src\Aloe\Apps\SyncBridge\Aloe.Apps.SyncBridge\bin\Debug\SyncBridge.exe
```

See `test/README.md` for detailed test scenarios.

## Code Conventions

- Uses .NET Framework 4.6.1 (old-style .csproj format)
- Console messages are in Japanese
- Interface-based architecture (all services have I-prefixed interfaces)
- Manual dependency injection in `Program.cs`
- File sync uses timestamp comparison (not hash-based)
- Environment variables in manifest paths are auto-expanded by `IniManifestRepository`
- **Zero external dependencies**: Uses Win32 API for INI parsing (no NuGet packages)

## Important Implementation Details

- **Process Launch**: Uses `UseShellExecute = false` to directly invoke `dotnet.exe` with the application DLL path
- **Environment Variables**: Can set `DOTNET_ROOT_X64` via `LaunchContext`
- **File Sync**: Compares `LastWriteTime`, copies if source is newer
- **Rollback**: Not yet implemented (mentioned as future feature in README)
- **No logging framework**: Currently uses `Console.WriteLine()` with Japanese prefixes like `[情報]`, `[エラー]`
