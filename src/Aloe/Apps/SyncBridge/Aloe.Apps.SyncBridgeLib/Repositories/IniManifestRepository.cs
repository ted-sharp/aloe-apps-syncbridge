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

        public SyncManifest LoadManifest()
        {
            string manifestPath = GetManifestPath();

            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"マニフェストファイルが見つかりません: {manifestPath}");
            }

            var manifest = new SyncManifest
            {
                Version = GetValue(manifestPath, "Manifest", "Version"),
                SourceRootPath = Environment.ExpandEnvironmentVariables(GetValue(manifestPath, "Manifest", "SourceRootPath")),
                LocalBasePath = Environment.ExpandEnvironmentVariables(GetValue(manifestPath, "Manifest", "LocalBasePath")),
                Runtime = LoadRuntime(manifestPath),
                SyncOptions = LoadSyncOptions(manifestPath),
                Applications = LoadApplications(manifestPath)
            };

            return manifest;
        }

        private RuntimeConfig LoadRuntime(string manifestPath)
        {
            return new RuntimeConfig
            {
                RelativePath = GetValue(manifestPath, "Runtime", "RelativePath"),
                ZipFileName = GetValue(manifestPath, "Runtime", "ZipFileName", "")
            };
        }

        private SyncOptions LoadSyncOptions(string manifestPath)
        {
            var options = new SyncOptions();

            string skipPatterns = GetValue(manifestPath, "SyncOptions", "SkipPatterns", "");
            if (!string.IsNullOrEmpty(skipPatterns))
            {
                options.SkipPatterns = skipPatterns
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList();
            }

            return options;
        }

        private List<AppConfig> LoadApplications(string manifestPath)
        {
            var apps = new List<AppConfig>();
            var sectionNames = GetAllSectionNames(manifestPath);

            foreach (var sectionName in sectionNames.Where(s => s.StartsWith("App.")))
            {
                string appId = sectionName.Substring(4);

                var app = new AppConfig
                {
                    AppId = appId,
                    RelativePath = GetValue(manifestPath, sectionName, "RelativePath"),
                    EntryDll = GetValue(manifestPath, sectionName, "EntryDll"),
                    LaunchArgPattern = GetValue(manifestPath, sectionName, "LaunchArgPattern", ""),
                    ZipFileName = GetValue(manifestPath, sectionName, "ZipFileName", "")
                };

                apps.Add(app);
            }

            return apps;
        }

        private List<string> GetAllSectionNames(string manifestPath)
        {
            IntPtr buffer = Marshal.AllocHGlobal(BufferSize * 2);
            try
            {
                int length = GetPrivateProfileSectionNames(buffer, BufferSize, manifestPath);
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

        private string GetValue(string manifestPath, string section, string key, string defaultValue = "")
        {
            var sb = new StringBuilder(BufferSize);
            GetPrivateProfileString(section, key, defaultValue, sb, BufferSize, manifestPath);
            return sb.ToString();
        }

        private string GetManifestPath()
        {
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            return Path.Combine(exeDir, ManifestFileName);
        }
    }
}
