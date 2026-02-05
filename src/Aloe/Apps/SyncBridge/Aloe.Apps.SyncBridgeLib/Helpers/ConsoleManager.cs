using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Aloe.Apps.SyncBridgeLib.Helpers
{
    /// <summary>
    /// コンソールウィンドウの動的制御を行うクラス
    /// </summary>
    public static class ConsoleManager
    {
        private static bool _consoleAllocated = false;
        private static readonly object _lock = new object();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// コンソールが割り当てられているかどうか
        /// </summary>
        public static bool IsConsoleAllocated
        {
            get { return _consoleAllocated; }
        }

        /// <summary>
        /// コンソールが表示されていることを保証する
        /// スレッドセーフ
        /// </summary>
        public static void EnsureConsoleVisible()
        {
            lock (_lock)
            {
                if (_consoleAllocated)
                {
                    return;
                }

                // まず親プロセス(cmd.exe等)のコンソールへのアタッチを試みる
                if (AttachConsole(ATTACH_PARENT_PROCESS))
                {
                    _consoleAllocated = true;
                    InitializeConsoleStreams();
                    return;
                }

                // 親プロセスのコンソールがない場合は新規作成
                if (AllocConsole())
                {
                    _consoleAllocated = true;
                    InitializeConsoleStreams();
                }
            }
        }

        /// <summary>
        /// コンソールストリームを初期化する
        /// </summary>
        private static void InitializeConsoleStreams()
        {
            // 標準出力と標準エラー出力を再初期化
            try
            {
                Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
            }
            catch
            {
                // ストリーム初期化に失敗しても続行
            }
        }
    }
}
