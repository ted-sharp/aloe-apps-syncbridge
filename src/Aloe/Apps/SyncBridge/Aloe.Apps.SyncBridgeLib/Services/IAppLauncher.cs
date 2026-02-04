namespace Aloe.Apps.SyncBridgeLib.Services
{
    public interface IAppLauncher
    {
        void Launch(LaunchContext context);
    }

    public class LaunchContext
    {
        public string DotnetExePath { get; set; }
        public string AppDllPath { get; set; }
        public string WorkingDirectory { get; set; }
        public string DotnetRootPath { get; set; }
        public string[] Arguments { get; set; }
    }
}
