using System;
using System.IO;
using Aloe.Apps.SyncBridgeLib.Helpers;
using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class SyncOrchestrator : ISyncOrchestrator
    {
        private readonly IFileSynchronizer _fileSynchronizer;
        private readonly IZipExtractor _zipExtractor;

        public SyncOrchestrator(IFileSynchronizer fileSynchronizer, IZipExtractor zipExtractor)
        {
            _fileSynchronizer = fileSynchronizer;
            _zipExtractor = zipExtractor;
        }

        public SyncOrchestratorResult SyncAll(SyncManifest manifest)
        {
            var result = new SyncOrchestratorResult();

            try
            {
                Console.WriteLine($"[情報] 同期開始: {manifest.SourceRootPath} -> {manifest.LocalBasePath}");

                var skipPatterns = manifest.SyncOptions?.SkipPatterns?.ToArray();

                var manifestResult = SyncManifestFile(manifest);
                if (!manifestResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = manifestResult.ErrorMessage;
                    return result;
                }
                result.TotalFilesUpdated += manifestResult.FilesUpdated;
                result.TotalFilesSkipped += manifestResult.FilesSkipped;

                if (manifestResult.FilesUpdated > 0)
                {
                    ConsoleManager.EnsureConsoleVisible();
                }

                var runtimeResult = SyncRuntime(manifest, skipPatterns);
                if (!runtimeResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = runtimeResult.ErrorMessage;
                    return result;
                }
                result.TotalFilesUpdated += runtimeResult.FilesUpdated;
                result.TotalFilesSkipped += runtimeResult.FilesSkipped;

                if (runtimeResult.FilesUpdated > 0)
                {
                    ConsoleManager.EnsureConsoleVisible();
                }
                Console.WriteLine($"[情報] ランタイム同期: {runtimeResult.FilesUpdated}ファイル更新");

                foreach (var app in manifest.Applications)
                {
                    var appResult = SyncApplication(manifest, app, skipPatterns);
                    if (!appResult.Success)
                    {
                        result.Success = false;
                        result.ErrorMessage = appResult.ErrorMessage;
                        return result;
                    }
                    result.TotalFilesUpdated += appResult.FilesUpdated;
                    result.TotalFilesSkipped += appResult.FilesSkipped;

                    if (appResult.FilesUpdated > 0)
                    {
                        ConsoleManager.EnsureConsoleVisible();
                    }
                    Console.WriteLine($"[情報] {app.AppId}同期: {appResult.FilesUpdated}ファイル更新");
                }

                Console.WriteLine($"[情報] 同期完了: 合計{result.TotalFilesUpdated}ファイル");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private SyncResult SyncManifestFile(SyncManifest manifest)
        {
            var result = new SyncResult();

            try
            {
                string sourceManifest = Path.Combine(manifest.SourceRootPath, "manifest.json");
                string targetManifest = Path.Combine(manifest.LocalBasePath, "manifest.json");

                if (File.Exists(sourceManifest))
                {
                    Directory.CreateDirectory(manifest.LocalBasePath);
                    File.Copy(sourceManifest, targetManifest, true);
                    result.FilesUpdated = 1;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"マニフェスト同期エラー: {ex.Message}";
            }

            return result;
        }

        private SyncResult SyncRuntime(SyncManifest manifest, string[] skipPatterns)
        {
            string runtimeZip = manifest.Runtime.ZipFileName;

            if (!string.IsNullOrEmpty(runtimeZip))
            {
                return SyncFromZip(
                    zipFileName: runtimeZip,
                    relativePath: manifest.Runtime.RelativePath,
                    sourceRoot: manifest.SourceRootPath,
                    localBase: manifest.LocalBasePath,
                    skipPatterns: skipPatterns
                );
            }
            else
            {
                string sourcePath = Path.Combine(manifest.SourceRootPath, manifest.Runtime.RelativePath);
                string targetPath = Path.Combine(manifest.LocalBasePath, manifest.Runtime.RelativePath);
                return _fileSynchronizer.SyncFolder(sourcePath, targetPath, skipPatterns);
            }
        }

        private SyncResult SyncApplication(SyncManifest manifest, AppConfig app, string[] skipPatterns)
        {
            string appZip = app.ZipFileName;

            if (!string.IsNullOrEmpty(appZip))
            {
                return SyncFromZip(
                    zipFileName: appZip,
                    relativePath: app.RelativePath,
                    sourceRoot: manifest.SourceRootPath,
                    localBase: manifest.LocalBasePath,
                    skipPatterns: skipPatterns
                );
            }
            else
            {
                string sourcePath = Path.Combine(manifest.SourceRootPath, app.RelativePath);
                string targetPath = Path.Combine(manifest.LocalBasePath, app.RelativePath);
                return _fileSynchronizer.SyncFolder(sourcePath, targetPath, skipPatterns);
            }
        }

        private SyncResult SyncFromZip(string zipFileName, string relativePath, string sourceRoot, string localBase, string[] skipPatterns)
        {
            var result = new SyncResult();

            try
            {
                string zipFilePath = Path.Combine(sourceRoot, zipFileName);
                string targetDirectory = Path.Combine(localBase, relativePath);

                if (!File.Exists(zipFilePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"ZIPファイルが見つかりません: {zipFilePath}";
                    Console.WriteLine($"[エラー] {result.ErrorMessage}");
                    return result;
                }

                var decision = _zipExtractor.DetermineSyncStrategy(zipFilePath, targetDirectory);
                Console.WriteLine($"[情報] ZIP同期戦略: {decision.Strategy} - {decision.Reason}");

                switch (decision.Strategy)
                {
                    case ZipSyncStrategy.InitialExtraction:
                    case ZipSyncStrategy.ReExtraction:
                        var extractionResult = _zipExtractor.ExtractIfNeeded(zipFilePath, targetDirectory, forceExtraction: true);
                        if (!extractionResult.Success)
                        {
                            result.Success = false;
                            result.ErrorMessage = extractionResult.ErrorMessage;
                            Console.WriteLine($"[エラー] ZIP展開失敗: {extractionResult.ErrorMessage}");
                            return result;
                        }
                        result.FilesUpdated = extractionResult.FilesExtracted;
                        break;

                    case ZipSyncStrategy.FolderSync:
                        string sourceFolderPath = Path.Combine(sourceRoot, relativePath);
                        if (Directory.Exists(sourceFolderPath))
                        {
                            var syncResult = _fileSynchronizer.SyncFolder(sourceFolderPath, targetDirectory, skipPatterns);
                            result.FilesUpdated = syncResult.FilesUpdated;
                            result.FilesSkipped = syncResult.FilesSkipped;
                            result.Success = syncResult.Success;
                            result.ErrorMessage = syncResult.ErrorMessage;
                        }
                        else
                        {
                            Console.WriteLine($"[情報] 差分同期用のソースフォルダが見つかりません: {sourceFolderPath}");
                            result.FilesSkipped = 0;
                        }
                        break;

                    case ZipSyncStrategy.Skip:
                        result.FilesSkipped = 0;
                        break;

                    default:
                        result.Success = false;
                        result.ErrorMessage = $"不明な同期戦略: {decision.Strategy}";
                        break;
                }

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"ZIP同期エラー: {ex.Message}";
                Console.WriteLine($"[エラー] {result.ErrorMessage}");
            }

            return result;
        }
    }
}
