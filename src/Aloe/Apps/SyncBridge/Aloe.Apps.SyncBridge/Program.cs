using System;
using System.Threading;
using Aloe.Apps.SyncBridgeLib.Helpers;
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
                // ClickOnce URL引数を統合
                args = ClickOnceHelper.MergeArguments(
                    args,
                    ClickOnceHelper.GetClickOnceArguments());

                var options = CommandLineOptions.Parse(args);

                if (options.RandomDelaySeconds > 0)
                {
                    var random = new Random();
                    var delayMs = random.Next(0, options.RandomDelaySeconds * 1000);
                    Thread.Sleep(delayMs);
                }

                if (options.ShowConsole)
                {
                    ConsoleManager.EnsureConsoleVisible();
                    Console.WriteLine("[情報] 統合後の引数:");
                    foreach (var arg in args)
                    {
                        Console.WriteLine($"  {arg}");
                    }
                }

                Console.WriteLine("[情報] SyncBridge 開始");

                var manifestRepo = new IniManifestRepository();
                var fileSynchronizer = new FileSynchronizer();
                var zipExtractor = new ZipExtractor();
                var syncOrchestrator = new SyncOrchestrator(fileSynchronizer, zipExtractor);
                var appSelector = new AppSelector();
                var appLauncher = new AppLauncher();

                var bootstrapper = new SyncBridgeBootstrapper(
                    manifestRepo,
                    syncOrchestrator,
                    appSelector,
                    appLauncher
                );

                if (options.SyncOnly)
                {
                    bootstrapper.ExecuteSyncOnly(args);
                }
                else
                {
                    bootstrapper.Execute(args);
                }

                Console.WriteLine("[情報] SyncBridge 終了");
            }
            catch (Exception ex)
            {
                ConsoleManager.EnsureConsoleVisible();
                Console.WriteLine($"[エラー] {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Environment.Exit(1);
            }
        }
    }
}
