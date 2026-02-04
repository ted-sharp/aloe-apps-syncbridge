using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Repositories
{
    public interface IManifestRepository
    {
        SyncManifest LoadManifest();
    }
}
