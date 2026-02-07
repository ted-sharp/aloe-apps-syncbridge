using System;
using System.Linq;

namespace Aloe.Apps.SyncBridgeLib.Helpers
{
    /// <summary>
    /// コマンドライン引数をパースして保持するクラス
    /// </summary>
    public class CommandLineOptions
    {
        private string[] _rawArgs;

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
            options._rawArgs = args ?? new string[0];

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

        /// <summary>
        /// SyncBridge固有の引数を除外し、アプリケーションに渡す引数のみを取得する
        /// </summary>
        /// <returns>アプリケーション用の引数配列</returns>
        public string[] GetApplicationArguments()
        {
            var result = new System.Collections.Generic.List<string>();

            foreach (var arg in _rawArgs)
            {
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }

                var trimmedArg = arg.Trim();

                // SyncBridge固有の引数は除外
                if (trimmedArg.Equals("--console", StringComparison.OrdinalIgnoreCase) ||
                    trimmedArg.Equals("--sync", StringComparison.OrdinalIgnoreCase) ||
                    trimmedArg.StartsWith("--random-delay=", StringComparison.OrdinalIgnoreCase) ||
                    trimmedArg.StartsWith("--app=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                result.Add(arg);
            }

            return result.ToArray();
        }
    }
}
