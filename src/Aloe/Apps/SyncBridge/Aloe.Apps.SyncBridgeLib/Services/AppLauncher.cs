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

            Console.WriteLine($"[情報] 起動: {context.AppDisplayName} (v{context.AppVersion})");

            var startInfo = new ProcessStartInfo
            {
                FileName = context.DotnetExePath,
                Arguments = BuildArguments(context),
                UseShellExecute = false,
                WorkingDirectory = context.WorkingDirectory
            };

            startInfo.EnvironmentVariables["DOTNET_ROOT_X64"] = context.DotnetRootPath;

            var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException("プロセスの起動に失敗しました");
            }
        }

        private string BuildArguments(LaunchContext context)
        {
            string args = $"\"{context.AppDllPath}\"";

            if (context.Arguments != null && context.Arguments.Length > 0)
            {
                args += " " + string.Join(" ", context.Arguments);
            }

            return args;
        }
    }
}
