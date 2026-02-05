using System;
using System.Linq;

namespace Aloe.Apps.SyncBridgeLib.Helpers
{
    /// <summary>
    /// コマンドライン引数をパースして保持するクラス
    /// </summary>
    public class CommandLineOptions
    {
        /// <summary>
        /// --consoleフラグ: コンソールを明示的に表示する
        /// </summary>
        public bool ShowConsole { get; private set; }

        /// <summary>
        /// --syncフラグ: 同期のみ実行、アプリ起動をスキップ
        /// </summary>
        public bool SyncOnly { get; private set; }

        /// <summary>
        /// --random-delay=N: 起動前に0～N秒のランダム待機
        /// </summary>
        public int RandomDelaySeconds { get; private set; }

        /// <summary>
        /// --app=AppId: 起動するアプリケーションID
        /// </summary>
        public string AppId { get; private set; }

        /// <summary>
        /// コマンドライン引数をパースする
        /// </summary>
        /// <param name="args">コマンドライン引数</param>
        /// <returns>パースされたオプション</returns>
        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            if (args == null || args.Length == 0)
            {
                return options;
            }

            foreach (var arg in args)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                var trimmedArg = arg.Trim();

                if (trimmedArg.Equals("--console", StringComparison.OrdinalIgnoreCase))
                {
                    options.ShowConsole = true;
                }
                else if (trimmedArg.Equals("--sync", StringComparison.OrdinalIgnoreCase))
                {
                    options.SyncOnly = true;
                }
                else if (trimmedArg.StartsWith("--random-delay=", StringComparison.OrdinalIgnoreCase))
                {
                    var valueStr = trimmedArg.Substring("--random-delay=".Length);
                    int delaySeconds;
                    if (int.TryParse(valueStr, out delaySeconds) && delaySeconds >= 0)
                    {
                        options.RandomDelaySeconds = delaySeconds;
                    }
                    else
                    {
                        Console.WriteLine("[警告] --random-delay の値が不正です: {0}", valueStr);
                    }
                }
                else if (trimmedArg.StartsWith("--app=", StringComparison.OrdinalIgnoreCase))
                {
                    options.AppId = trimmedArg.Substring("--app=".Length);
                }
            }

            return options;
        }
    }
}
