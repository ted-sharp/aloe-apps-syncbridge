using System;
using System.Linq;
using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class AppSelector : IAppSelector
    {
        public AppConfig SelectApp(string appId, SyncManifest manifest)
        {
            if (manifest.Applications == null || manifest.Applications.Count == 0)
            {
                throw new InvalidOperationException("マニフェストにアプリケーションが定義されていません");
            }

            if (!string.IsNullOrEmpty(appId))
            {
                var app = manifest.Applications.FirstOrDefault(a =>
                    a.AppId.Equals(appId, StringComparison.OrdinalIgnoreCase));

                if (app != null)
                {
                    return app;
                }

                Console.WriteLine($"[警告] 指定されたアプリ '{appId}' が見つかりません。最初のアプリを使用します。");
            }

            return manifest.Applications[0];
        }
    }
}
