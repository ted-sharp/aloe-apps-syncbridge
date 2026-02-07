using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class ZipExtractor : IZipExtractor
    {
        private const string MarkerFileName = ".zip-extracted";

        public ZipSyncDecision DetermineSyncStrategy(string zipFilePath, string targetDirectory)
        {
            if (!File.Exists(zipFilePath))
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.NotApplicable,
                    Reason = "ZIPファイルが見つかりません"
                };
            }

            string markerPath = Path.Combine(targetDirectory, MarkerFileName);

            if (!File.Exists(markerPath))
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.InitialExtraction,
                    Reason = "マーカーファイルが存在しないため、初回展開を実行します"
                };
            }

            var zipTimestamp = File.GetLastWriteTimeUtc(zipFilePath);
            var markerInfo = ReadMarkerFile(markerPath);

            if (markerInfo == null || string.IsNullOrEmpty(markerInfo.ZipTimestamp))
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.InitialExtraction,
                    Reason = "マーカーファイルが不正なため、初回展開を実行します"
                };
            }

            DateTime markerTimestamp;
            if (!DateTime.TryParse(markerInfo.ZipTimestamp, out markerTimestamp))
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.InitialExtraction,
                    Reason = "マーカーファイルのタイムスタンプが不正なため、初回展開を実行します"
                };
            }

            markerTimestamp = DateTime.SpecifyKind(markerTimestamp, DateTimeKind.Utc);

            var zipSeconds = new DateTimeOffset(zipTimestamp).ToUnixTimeSeconds();
            var markerSeconds = new DateTimeOffset(markerTimestamp).ToUnixTimeSeconds();

            if (zipSeconds > markerSeconds)
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.ReExtraction,
                    Reason = $"ZIPファイルが更新されています (ZIP: {zipTimestamp:yyyy-MM-dd HH:mm:ss}, マーカー: {markerTimestamp:yyyy-MM-dd HH:mm:ss})"
                };
            }
            else if (zipSeconds == markerSeconds)
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.FolderSync,
                    Reason = "ZIPファイルに変更がないため、フォルダ差分同期を実行します"
                };
            }
            else
            {
                return new ZipSyncDecision
                {
                    Strategy = ZipSyncStrategy.Skip,
                    Reason = "ZIPファイルに変更がありません"
                };
            }
        }

        public ZipExtractionResult ExtractIfNeeded(string zipFilePath, string targetDirectory, bool forceExtraction = false)
        {
            var result = new ZipExtractionResult { Success = false, FilesExtracted = 0 };

            try
            {
                if (!File.Exists(zipFilePath))
                {
                    result.ErrorMessage = $"ZIPファイルが見つかりません: {zipFilePath}";
                    return result;
                }

                if (Directory.Exists(targetDirectory) && (forceExtraction || IsReExtractionNeeded(zipFilePath, targetDirectory)))
                {
                    Console.WriteLine($"[情報] 既存のディレクトリを削除します: {targetDirectory}");
                    Directory.Delete(targetDirectory, true);
                }

                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                Console.WriteLine($"[情報] ZIPファイルを展開しています: {zipFilePath}");

                string normalizedTargetDir = Path.GetFullPath(targetDirectory);

                using (var archive = ZipFile.OpenRead(zipFilePath))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.Combine(targetDirectory, entry.FullName);
                        string normalizedDestPath = Path.GetFullPath(destinationPath);

                        // パストラバーサル攻撃を防ぐ: 展開先がtargetDirectory配下にあることを確認
                        if (!normalizedDestPath.StartsWith(normalizedTargetDir + Path.DirectorySeparatorChar) &&
                            !normalizedDestPath.Equals(normalizedTargetDir))
                        {
                            result.ErrorMessage = $"不正なパスが含まれています: {entry.FullName}";
                            return result;
                        }

                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, true);
                            result.FilesExtracted++;
                        }
                    }
                }

                WriteMarkerFile(targetDirectory, Path.GetFileName(zipFilePath), File.GetLastWriteTimeUtc(zipFilePath));

                Console.WriteLine($"[情報] {result.FilesExtracted} 個のファイルを展開しました");

                result.Success = true;
                return result;
            }
            catch (InvalidDataException ex)
            {
                result.ErrorMessage = $"ZIPファイルが破損しています: {ex.Message}";
                return result;
            }
            catch (IOException ex)
            {
                result.ErrorMessage = $"入出力エラーが発生しました: {ex.Message}";
                return result;
            }
            catch (UnauthorizedAccessException ex)
            {
                result.ErrorMessage = $"アクセスが拒否されました: {ex.Message}";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"予期しないエラーが発生しました: {ex.Message}";
                return result;
            }
        }

        private bool IsReExtractionNeeded(string zipFilePath, string targetDirectory)
        {
            string markerPath = Path.Combine(targetDirectory, MarkerFileName);
            if (!File.Exists(markerPath))
            {
                return false;
            }

            var decision = DetermineSyncStrategy(zipFilePath, targetDirectory);
            return decision.Strategy == ZipSyncStrategy.ReExtraction;
        }

        private MarkerFileInfo ReadMarkerFile(string markerPath)
        {
            try
            {
                var lines = File.ReadAllLines(markerPath);
                var info = new MarkerFileInfo();

                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (key == "ZipFileName")
                    {
                        info.ZipFileName = value;
                    }
                    else if (key == "ZipTimestampUtc")
                    {
                        info.ZipTimestamp = value;
                    }
                    else if (key == "ExtractedAtUtc")
                    {
                        info.ExtractedAt = value;
                    }
                }

                return info;
            }
            catch
            {
                return null;
            }
        }

        private void WriteMarkerFile(string targetDirectory, string zipFileName, DateTime zipTimestamp)
        {
            string markerPath = Path.Combine(targetDirectory, MarkerFileName);
            var lines = new[]
            {
                $"ZipFileName={zipFileName}",
                $"ZipTimestampUtc={zipTimestamp:yyyy-MM-ddTHH:mm:ssZ}",
                $"ExtractedAtUtc={DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}"
            };

            File.WriteAllLines(markerPath, lines);
        }

        private class MarkerFileInfo
        {
            public string ZipFileName { get; set; }
            public string ZipTimestamp { get; set; }
            public string ExtractedAt { get; set; }
        }
    }
}
