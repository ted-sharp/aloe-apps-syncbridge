using System;
using Aloe.Apps.SyncBridgeLib.Repositories;
using Aloe.Apps.SyncBridgeLib.Services;

namespace Aloe.Apps.SyncBridge
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("[情報] SyncBridge 開始");

                var manifestRepo = new JsonManifestRepository();
                var fileSynchronizer = new FileSynchronizer();
                var syncOrchestrator = new SyncOrchestrator(fileSynchronizer);
                var appSelector = new AppSelector();
                var appLauncher = new AppLauncher();

                var bootstrapper = new SyncBridgeBootstrapper(
                    manifestRepo,
                    syncOrchestrator,
                    appSelector,
                    appLauncher
                );

                bootstrapper.Execute(args);

                Console.WriteLine("[情報] SyncBridge 終了");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[エラー] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
