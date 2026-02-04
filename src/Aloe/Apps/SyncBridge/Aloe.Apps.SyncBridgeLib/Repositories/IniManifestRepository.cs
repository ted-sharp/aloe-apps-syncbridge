using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Aloe.Apps.SyncBridgeLib.Models;

namespace Aloe.Apps.SyncBridgeLib.Repositories
{
    public class IniManifestRepository : IManifestRepository
    {
        private const string ManifestFileName = "manifest.ini";
        private const int BufferSize = 32767;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(
            string section,
            string key,
            string defaultValue,
            StringBuilder returnValue,
            int size,
            string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileSectionNames(
            IntPtr returnValue,
            int size,
            string filePath);

        private string _manifestPath;

        public SyncManifest LoadManifest()
        {
            _manifestPath = GetManifestPath();

            if (!File.Exists(_manifestPath))
            {
                throw new FileNotFoundException($"マニフェストファイルが見つかりません: {_manifestPath}");
            }

            var manifest = new SyncManifest
            {
                Version = GetValue("Manifest", "Version"),
                SourceRootPath = Environment.ExpandEnvironmentVariables(GetValue("Manifest", "SourceRootPath")),
                LocalBasePath = Environment.ExpandEnvironmentVariables(GetValue("Manifest", "LocalBasePath")),
                Runtime = LoadRuntime(),
                SyncOptions = LoadSyncOptions(),
                Applications = LoadApplications()
            };

            return manifest;
        }

        private RuntimeConfig LoadRuntime()
        {
            return new RuntimeConfig
            {
                Version = GetValue("Runtime", "Version"),
                RelativePath = GetValue("Runtime", "RelativePath")
            };
        }

        private SyncOptions LoadSyncOptions()
        {
            var options = new SyncOptions
            {
                RetryCount = int.Parse(GetValue("SyncOptions", "RetryCount", "3")),
                TimeoutSeconds = int.Parse(GetValue("SyncOptions", "TimeoutSeconds", "300"))
            };

            string skipPatterns = GetValue("SyncOptions", "SkipPatterns", "");
            if (!string.IsNullOrEmpty(skipPatterns))
            {
                options.SkipPatterns = skipPatterns
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList();
            }

            return options;
        }

        private List<AppConfig> LoadApplications()
        {
            var apps = new List<AppConfig>();
            var sectionNames = GetAllSectionNames();

            foreach (var sectionName in sectionNames.Where(s => s.StartsWith("App.")))
            {
                string appId = sectionName.Substring(4);

                var app = new AppConfig
                {
                    AppId = appId,
                    DisplayName = GetValue(sectionName, "DisplayName"),
                    Version = GetValue(sectionName, "Version"),
                    RelativePath = GetValue(sectionName, "RelativePath"),
                    EntryDll = GetValue(sectionName, "EntryDll"),
                    LaunchArgPattern = GetValue(sectionName, "LaunchArgPattern", "")
                };

                apps.Add(app);
            }

            return apps;
        }

        private List<string> GetAllSectionNames()
        {
            IntPtr buffer = Marshal.AllocHGlobal(BufferSize * 2);
            try
            {
                int length = GetPrivateProfileSectionNames(buffer, BufferSize, _manifestPath);
                if (length == 0)
                    return new List<string>();

                string result = Marshal.PtrToStringUni(buffer, length - 1);
                return result.Split('\0').Where(s => !string.IsNullOrEmpty(s)).ToList();
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private string GetValue(string section, string key, string defaultValue = "")
        {
            var sb = new StringBuilder(BufferSize);
            GetPrivateProfileString(section, key, defaultValue, sb, BufferSize, _manifestPath);
            return sb.ToString();
        }

        private string GetManifestPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string localManifestPath = Path.Combine(localAppData, "Company", "SyncBridge", ManifestFileName);

            if (File.Exists(localManifestPath))
            {
                return localManifestPath;
            }

            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string fallbackPath = Path.Combine(exeDir, ManifestFileName);

            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }

            return localManifestPath;
        }
    }
}
