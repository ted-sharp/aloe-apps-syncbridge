using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public interface ISyncOrchestrator
    {
        SyncOrchestratorResult SyncAll(SyncManifest manifest);
    }

    public class SyncOrchestratorResult
    {
        public int TotalFilesUpdated { get; set; }
        public int TotalFilesSkipped { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public SyncOrchestratorResult()
        {
            Success = true;
        }
    }
}
