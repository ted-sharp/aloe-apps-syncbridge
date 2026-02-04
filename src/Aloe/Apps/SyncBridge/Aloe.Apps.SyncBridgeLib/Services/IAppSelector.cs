using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public interface IAppSelector
    {
        AppConfig SelectApp(string[] args, SyncManifest manifest);
    }
}
