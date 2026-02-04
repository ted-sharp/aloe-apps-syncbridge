namespace Aloe.Apps.SyncBridgeLib.Services
{
    public interface IFileSynchronizer
    {
        SyncResult SyncFolder(string sourcePath, string targetPath, string[] skipPatterns = null);
    }

    public class SyncResult
    {
        public int FilesUpdated { get; set; }
        public int FilesSkipped { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public SyncResult()
        {
            Success = true;
        }
    }
}
