using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

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

            // dotnet.exeを直接起動し、環境変数で DOTNET_ROOT_X64 を設定
            // コマンドインジェクションリスクを回避するため、引数を適切にエスケープ
            var startInfo = new ProcessStartInfo
            {
                FileName = context.DotnetExePath,
                Arguments = BuildArgumentsString(context.AppDllPath, context.Arguments),
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = context.WorkingDirectory
            };

            // 環境変数を設定
            startInfo.EnvironmentVariables["DOTNET_ROOT_X64"] = context.DotnetRootPath;

            var process = Process.Start(startInfo);

            if (process == null)
            {
                throw new InvalidOperationException("プロセスの起動に失敗しました");
            }

            // プロセスが起動するまで少し待機
            process.WaitForExit(1000);
        }

        /// <summary>
        /// コマンドライン引数文字列を構築（適切にエスケープ）
        /// </summary>
        private string BuildArgumentsString(string appDllPath, string[] appArguments)
        {
            var sb = new StringBuilder();

            // DLLパスを追加（必ずクォートで囲む）
            sb.Append(EscapeArgument(appDllPath));

            // アプリケーション引数を追加
            if (appArguments != null)
            {
                foreach (var arg in appArguments)
                {
                    sb.Append(" ");
                    sb.Append(EscapeArgument(arg));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// コマンドライン引数をエスケープ
        /// Windows のコマンドライン引数規則に従う
        /// </summary>
        private string EscapeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            // スペース、タブ、ダブルクォートを含む場合はクォートが必要
            bool needsQuotes = argument.Any(c => c == ' ' || c == '\t' || c == '"');

            if (!needsQuotes)
            {
                return argument;
            }

            var sb = new StringBuilder();
            sb.Append('"');

            int backslashCount = 0;
            foreach (char c in argument)
            {
                if (c == '\\')
                {
                    backslashCount++;
                }
                else if (c == '"')
                {
                    // バックスラッシュをエスケープ（ダブルクォート前）
                    sb.Append('\\', backslashCount * 2 + 1);
                    sb.Append('"');
                    backslashCount = 0;
                }
                else
                {
                    // 通常のバックスラッシュを出力
                    sb.Append('\\', backslashCount);
                    sb.Append(c);
                    backslashCount = 0;
                }
            }

            // 末尾のバックスラッシュをエスケープ（クォート前）
            sb.Append('\\', backslashCount * 2);
            sb.Append('"');

            return sb.ToString();
        }
    }
}
