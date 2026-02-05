using System;
using System.Collections.Generic;
using Aloe.Apps.SyncBridgeLib.Helpers;

namespace Aloe.Apps.SyncBridge.Tests
{
    /// <summary>
    /// ClickOnceHelper の動作確認用テストプログラム
    /// </summary>
    class TestClickOnceHelper
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ClickOnceHelper テスト ===\n");

            // テスト1: ConvertToCommandLineArgs
            Console.WriteLine("[テスト1] ConvertToCommandLineArgs");
            var parameters = new Dictionary<string, string>
            {
                { "app", "AppA" },
                { "param1", "value1" },
                { "param2", "test 123" }
            };
            var cmdArgs = ClickOnceHelper.ConvertToCommandLineArgs(parameters);
            Console.WriteLine("入力: app=AppA, param1=value1, param2=test 123");
            Console.WriteLine("結果:");
            foreach (var arg in cmdArgs)
            {
                Console.WriteLine("  " + arg);
            }
            Console.WriteLine();

            // テスト2: MergeArguments (重複なし)
            Console.WriteLine("[テスト2] MergeArguments (重複なし)");
            var cmdLine = new[] { "--console", "--sync" };
            var urlArgs = new[] { "--app=AppA", "--param1=value1" };
            var merged = ClickOnceHelper.MergeArguments(cmdLine, urlArgs);
            Console.WriteLine("コマンドライン: --console --sync");
            Console.WriteLine("URL引数: --app=AppA --param1=value1");
            Console.WriteLine("結果:");
            foreach (var arg in merged)
            {
                Console.WriteLine("  " + arg);
            }
            Console.WriteLine();

            // テスト3: MergeArguments (重複あり、コマンドライン優先)
            Console.WriteLine("[テスト3] MergeArguments (重複あり)");
            cmdLine = new[] { "--app=AppA", "--console" };
            urlArgs = new[] { "--app=AppB", "--param1=value1" };
            merged = ClickOnceHelper.MergeArguments(cmdLine, urlArgs);
            Console.WriteLine("コマンドライン: --app=AppA --console");
            Console.WriteLine("URL引数: --app=AppB --param1=value1");
            Console.WriteLine("結果 (AppAが優先される):");
            foreach (var arg in merged)
            {
                Console.WriteLine("  " + arg);
            }
            Console.WriteLine();

            // テスト4: IsClickOnceDeployment
            Console.WriteLine("[テスト4] IsClickOnceDeployment");
            bool isClickOnce = ClickOnceHelper.IsClickOnceDeployment();
            Console.WriteLine("結果: " + isClickOnce + " (非ClickOnce環境ではfalse)");
            Console.WriteLine();

            // テスト5: GetActivationParameters
            Console.WriteLine("[テスト5] GetActivationParameters");
            var activationParams = ClickOnceHelper.GetActivationParameters();
            Console.WriteLine("結果: " + activationParams.Count + "個のパラメータ (非ClickOnce環境では0)");
            Console.WriteLine();

            // テスト6: GetClickOnceArguments
            Console.WriteLine("[テスト6] GetClickOnceArguments");
            var clickOnceArgs = ClickOnceHelper.GetClickOnceArguments();
            Console.WriteLine("結果: " + clickOnceArgs.Length + "個の引数 (非ClickOnce環境では0)");
            Console.WriteLine();

            Console.WriteLine("=== テスト完了 ===");
        }
    }
}
