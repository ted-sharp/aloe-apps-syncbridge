using System.Collections.Generic;

namespace Aloe.Apps.SyncBridgeLib.Models
{
    public class SyncManifest
    {
        public string Version { get; set; }
        public string SourceRootPath { get; set; }
        public string LocalBasePath { get; set; }
        public RuntimeConfig Runtime { get; set; }
        public List<AppConfig> Applications { get; set; }
        public SyncOptions SyncOptions { get; set; }

        public SyncManifest()
        {
            Applications = new List<AppConfig>();
            SyncOptions = new SyncOptions();
        }
    }

    public class RuntimeConfig
    {
        public string RelativePath { get; set; }
    }

    public class AppConfig
    {
        public string AppId { get; set; }
        public string RelativePath { get; set; }
        public string EntryDll { get; set; }
        public string LaunchArgPattern { get; set; }
    }

    public class SyncOptions
    {
        public List<string> SkipPatterns { get; set; }

        public SyncOptions()
        {
            SkipPatterns = new List<string>();
        }
    }
}
