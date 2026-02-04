using System;
using System.IO;
using Aloe.Apps.SyncBridgeLib.Models;
using Newtonsoft.Json;

namespace Aloe.Apps.SyncBridgeLib.Repositories
{
    public class JsonManifestRepository : IManifestRepository
    {
        private const string ManifestFileName = "manifest.json";

        public SyncManifest LoadManifest()
        {
            string manifestPath = GetManifestPath();

            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"マニフェストファイルが見つかりません: {manifestPath}");
            }

            string json = File.ReadAllText(manifestPath);
            var manifest = JsonConvert.DeserializeObject<SyncManifest>(json);

            manifest.LocalBasePath = Environment.ExpandEnvironmentVariables(manifest.LocalBasePath);
            manifest.SourceRootPath = Environment.ExpandEnvironmentVariables(manifest.SourceRootPath);

            return manifest;
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
