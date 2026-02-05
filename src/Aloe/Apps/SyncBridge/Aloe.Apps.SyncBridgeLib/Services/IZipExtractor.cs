namespace Aloe.Apps.SyncBridgeLib.Services
{
    public interface IZipExtractor
    {
        ZipSyncDecision DetermineSyncStrategy(string zipFilePath, string targetDirectory);
        ZipExtractionResult ExtractIfNeeded(string zipFilePath, string targetDirectory, bool forceExtraction = false);
    }

    public enum ZipSyncStrategy
    {
        NotApplicable,
        InitialExtraction,
        ReExtraction,
        FolderSync,
        Skip
    }

    public class ZipSyncDecision
    {
        public ZipSyncStrategy Strategy { get; set; }
        public string Reason { get; set; }
    }

    public class ZipExtractionResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int FilesExtracted { get; set; }
    }
}
