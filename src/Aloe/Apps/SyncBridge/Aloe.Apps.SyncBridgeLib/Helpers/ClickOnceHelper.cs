using System;
using System.Collections.Generic;
using System.Linq;

namespace Aloe.Apps.SyncBridgeLib.Helpers
{
    /// <summary>
    /// ClickOnce環境でのURL引数（ActivationUri）を処理するヘルパークラス
    /// </summary>
    public static class ClickOnceHelper
    {
        /// <summary>
        /// ClickOnce URL引数を取得し、コマンドライン引数形式に変換
        /// </summary>
        /// <returns>--key=value 形式の引数配列</returns>
        public static string[] GetClickOnceArguments()
        {
            try
            {
                if (!IsClickOnceDeployment())
                {
                    return new string[0];
                }

                var parameters = GetActivationParameters();
                return ConvertToCommandLineArgs(parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] ClickOnce引数の取得に失敗: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// ClickOnce環境かどうかを判定
        /// </summary>
        public static bool IsClickOnceDeployment()
        {
            try
            {
                return System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ActivationUriからクエリパラメータを抽出
        /// </summary>
        public static Dictionary<string, string> GetActivationParameters()
        {
            var result = new Dictionary<string, string>();

            try
            {
                if (!System.Deployment.Application.ApplicationDeployment.IsNetworkDeployed)
                {
                    return result;
                }

                var deployment = System.Deployment.Application.ApplicationDeployment.CurrentDeployment;
                var activationUri = deployment.ActivationUri;

                if (activationUri == null)
                {
                    return result;
                }

                var query = activationUri.Query.TrimStart('?');
                if (string.IsNullOrEmpty(query))
                {
                    return result;
                }

                var pairs = query.Split('&');
                foreach (var pair in pairs)
                {
                    if (string.IsNullOrEmpty(pair)) continue;

                    var keyValue = pair.Split(new[] { '=' }, 2);
                    var key = Uri.UnescapeDataString(keyValue[0]);
                    var value = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]) : "";

                    result[key] = value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[警告] ActivationUriの解析に失敗: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Dictionaryを --key=value 形式の引数配列に変換
        /// </summary>
        public static string[] ConvertToCommandLineArgs(Dictionary<string, string> parameters)
        {
            return parameters
                .Select(kvp => $"--{kvp.Key}={kvp.Value}")
                .ToArray();
        }

        /// <summary>
        /// コマンドライン引数とURL引数を統合（コマンドライン引数を優先）
        /// </summary>
        /// <param name="commandLineArgs">コマンドライン引数</param>
        /// <param name="urlArgs">URL引数</param>
        /// <returns>統合された引数配列</returns>
        public static string[] MergeArguments(string[] commandLineArgs, string[] urlArgs)
        {
            var commandLineKeys = new HashSet<string>(
                commandLineArgs.Select(ExtractKey),
                StringComparer.OrdinalIgnoreCase
            );

            var nonDuplicateUrlArgs = urlArgs
                .Where(arg => !commandLineKeys.Contains(ExtractKey(arg)))
                .ToArray();

            return commandLineArgs.Concat(nonDuplicateUrlArgs).ToArray();
        }

        /// <summary>
        /// 引数からキーを抽出（--app=value → app）
        /// </summary>
        private static string ExtractKey(string arg)
        {
            if (string.IsNullOrEmpty(arg) || !arg.StartsWith("--"))
            {
                return arg;
            }

            var withoutPrefix = arg.Substring(2);
            var equalIndex = withoutPrefix.IndexOf('=');

            return equalIndex >= 0 ? withoutPrefix.Substring(0, equalIndex) : withoutPrefix;
        }
    }
}
