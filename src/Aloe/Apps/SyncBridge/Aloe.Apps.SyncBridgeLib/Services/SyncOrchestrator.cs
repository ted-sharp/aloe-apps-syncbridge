using System;
using System.IO;
using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class SyncOrchestrator : ISyncOrchestrator
    {
        private readonly IFileSynchronizer _fileSynchronizer;

        public SyncOrchestrator(IFileSynchronizer fileSynchronizer)
        {
            _fileSynchronizer = fileSynchronizer;
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

                var runtimeResult = SyncRuntime(manifest, skipPatterns);
                if (!runtimeResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = runtimeResult.ErrorMessage;
                    return result;
                }
                result.TotalFilesUpdated += runtimeResult.FilesUpdated;
                result.TotalFilesSkipped += runtimeResult.FilesSkipped;
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
            string sourcePath = Path.Combine(manifest.SourceRootPath, manifest.Runtime.RelativePath);
            string targetPath = Path.Combine(manifest.LocalBasePath, manifest.Runtime.RelativePath);

            return _fileSynchronizer.SyncFolder(sourcePath, targetPath, skipPatterns);
        }

        private SyncResult SyncApplication(SyncManifest manifest, AppConfig app, string[] skipPatterns)
        {
            string sourcePath = Path.Combine(manifest.SourceRootPath, app.RelativePath);
            string targetPath = Path.Combine(manifest.LocalBasePath, app.RelativePath);

            return _fileSynchronizer.SyncFolder(sourcePath, targetPath, skipPatterns);
        }
    }
}
