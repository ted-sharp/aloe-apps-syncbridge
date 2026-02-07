using System;
using System.IO;
using System.Linq;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    /// <summary>
    /// ファイル同期サービス
    /// 注意: このクラスはターゲットに存在するがソースに存在しないファイルの削除は行いません。
    /// ファイル追加・更新のみを処理します。
    /// </summary>
    public class FileSynchronizer : IFileSynchronizer
    {
        public SyncResult SyncFolder(string sourcePath, string targetPath, string[] skipPatterns = null)
        {
            var result = new SyncResult();

            try
            {
                if (!Directory.Exists(sourcePath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"同期元フォルダが存在しません: {sourcePath}";
                    return result;
                }

                Directory.CreateDirectory(targetPath);

                SyncDirectory(sourcePath, targetPath, skipPatterns, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private void SyncDirectory(string sourcePath, string targetPath, string[] skipPatterns, SyncResult result)
        {
            var sourceDir = new DirectoryInfo(sourcePath);
            var files = sourceDir.GetFiles();

            foreach (var file in files)
            {
                if (ShouldSkipFile(file.Name, skipPatterns))
                {
                    result.FilesSkipped++;
                    continue;
                }

                string targetFilePath = Path.Combine(targetPath, file.Name);

                if (ShouldCopyFile(file.FullName, targetFilePath))
                {
                    File.Copy(file.FullName, targetFilePath, true);
                    File.SetLastWriteTimeUtc(targetFilePath, file.LastWriteTimeUtc);
                    result.FilesUpdated++;
                }
                else
                {
                    result.FilesSkipped++;
                }
            }

            var directories = sourceDir.GetDirectories();
            foreach (var dir in directories)
            {
                string targetSubDir = Path.Combine(targetPath, dir.Name);
                Directory.CreateDirectory(targetSubDir);
                SyncDirectory(dir.FullName, targetSubDir, skipPatterns, result);
            }
        }

        private bool ShouldSkipFile(string fileName, string[] skipPatterns)
        {
            if (skipPatterns == null || skipPatterns.Length == 0)
            {
                return false;
            }

            foreach (var pattern in skipPatterns)
            {
                if (MatchesPattern(fileName, pattern))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchesPattern(string fileName, string pattern)
        {
            if (pattern.StartsWith("*"))
            {
                string extension = pattern.Substring(1);
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }

            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldCopyFile(string sourceFile, string targetFile)
        {
            if (!File.Exists(targetFile))
            {
                return true;
            }

            var sourceLastWrite = File.GetLastWriteTimeUtc(sourceFile);
            var targetLastWrite = File.GetLastWriteTimeUtc(targetFile);

            return sourceLastWrite > targetLastWrite;
        }
    }
}
