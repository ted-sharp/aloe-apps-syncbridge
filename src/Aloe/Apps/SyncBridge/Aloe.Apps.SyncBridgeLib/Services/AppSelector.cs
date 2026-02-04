using System;
using System.Linq;
using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class AppSelector : IAppSelector
    {
        public AppConfig SelectApp(string[] args, SyncManifest manifest)
        {
            if (manifest.Applications == null || manifest.Applications.Count == 0)
            {
                throw new InvalidOperationException("マニフェストにアプリケーションが定義されていません");
            }

            string targetAppId = FindAppIdFromArgs(args);

            if (!string.IsNullOrEmpty(targetAppId))
            {
                var app = manifest.Applications.FirstOrDefault(a =>
                    a.AppId.Equals(targetAppId, StringComparison.OrdinalIgnoreCase));

                if (app != null)
                {
                    return app;
                }

                Console.WriteLine($"[警告] 指定されたアプリ '{targetAppId}' が見つかりません。最初のアプリを使用します。");
            }

            return manifest.Applications[0];
        }

        private string FindAppIdFromArgs(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return null;
            }

            foreach (var arg in args)
            {
                if (arg.StartsWith("--app=", StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring("--app=".Length);
                }
            }

            return null;
        }
    }
}
