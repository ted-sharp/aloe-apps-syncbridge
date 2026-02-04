using System;
using System.Diagnostics;
using System.IO;

namespace Aloe.Apps.SyncBridgeLib.Services
{
    public class AppLauncher : IAppLauncher
    {
        public void Launch(LaunchContext context)
        {
            if (!File.Exists(context.DotnetExePath))
            {
                throw new FileNotFoundException($"dotnet.exeが見つかりません: {context.DotnetExePath}");
            }

            if (!File.Exists(context.AppDllPath))
            {
                throw new FileNotFoundException($"アプリケーションDLLが見つかりません: {context.AppDllPath}");
            }

            Console.WriteLine($"[情報] 起動: {context.AppDllPath}");

            // cmd.exe経由でstartコマンドを使用し、完全に独立したプロセスとして起動
            // これにより、SyncBridgeが終了してもアプリは終了しない
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = BuildStartCommand(context),
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = context.WorkingDirectory
            };

            var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException("プロセスの起動に失敗しました");
            }

            // プロセスが起動するまで少し待機
            process.WaitForExit(1000);
        }

        private string BuildStartCommand(LaunchContext context)
        {
            // startコマンドで新しいウィンドウを起動（親プロセスから独立）
            // 環境変数DOTNET_ROOT_X64を設定してからdotnetを起動
            string appArgs = "";
            if (context.Arguments != null && context.Arguments.Length > 0)
            {
                appArgs = " " + string.Join(" ", context.Arguments);
            }

            // /c: コマンドを実行して終了
            // set: 環境変数を設定
            // &&: 前のコマンドが成功したら次を実行
            // start "": 新しいウィンドウを起動（ウィンドウタイトルは空）
            // /B: 新しいウィンドウを作成しない（バックグラウンドで起動）
            return $"/c \"set DOTNET_ROOT_X64={context.DotnetRootPath} && cd /d \"{context.WorkingDirectory}\" && start /B \"\" \"{context.DotnetExePath}\" \"{context.AppDllPath}\"{appArgs}\"";
        }
    }
}
