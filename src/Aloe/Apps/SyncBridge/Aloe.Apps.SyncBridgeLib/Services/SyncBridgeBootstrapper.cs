using System;
using System.IO;
using Aloe.Apps.SyncBridgeLib.Models;
using Aloe.Apps.SyncBridgeLib.Repositories;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class SyncBridgeBootstrapper : ISyncBridgeBootstrapper
    {
        private readonly IManifestRepository _manifestRepository;
        private readonly ISyncOrchestrator _syncOrchestrator;
        private readonly IAppSelector _appSelector;
        private readonly IAppLauncher _appLauncher;

        public SyncBridgeBootstrapper(
            IManifestRepository manifestRepository,
            ISyncOrchestrator syncOrchestrator,
            IAppSelector appSelector,
            IAppLauncher appLauncher)
        {
            _manifestRepository = manifestRepository;
            _syncOrchestrator = syncOrchestrator;
            _appSelector = appSelector;
            _appLauncher = appLauncher;
        }

        public void Execute(string[] args)
        {
            Console.WriteLine("[情報] マニフェスト読み込み");
            var manifest = _manifestRepository.LoadManifest();
            Console.WriteLine("[情報] マニフェスト読み込み: OK");

            var syncResult = _syncOrchestrator.SyncAll(manifest);
            if (!syncResult.Success)
            {
                throw new InvalidOperationException($"同期エラー: {syncResult.ErrorMessage}");
            }

            var targetApp = _appSelector.SelectApp(args, manifest);

            var launchContext = BuildLaunchContext(manifest, targetApp, args);

            _appLauncher.Launch(launchContext);
        }

        public void ExecuteSyncOnly(string[] args)
        {
            Console.WriteLine("[情報] 同期専用モード");
            Console.WriteLine("[情報] マニフェスト読み込み");
            var manifest = _manifestRepository.LoadManifest();
            Console.WriteLine("[情報] マニフェスト読み込み: OK");

            var syncResult = _syncOrchestrator.SyncAll(manifest);
            if (!syncResult.Success)
            {
                throw new InvalidOperationException($"同期エラー: {syncResult.ErrorMessage}");
            }

            Console.WriteLine("[情報] 同期完了");
        }

        private LaunchContext BuildLaunchContext(SyncManifest manifest, AppConfig app, string[] args)
        {
            string runtimePath = Path.Combine(manifest.LocalBasePath, manifest.Runtime.RelativePath);
            string appPath = Path.Combine(manifest.LocalBasePath, app.RelativePath);

            return new LaunchContext
            {
                DotnetExePath = Path.Combine(runtimePath, "dotnet.exe"),
                AppDllPath = Path.Combine(appPath, app.EntryDll),
                WorkingDirectory = appPath,
                DotnetRootPath = runtimePath,
                Arguments = args
            };
        }
    }
}
