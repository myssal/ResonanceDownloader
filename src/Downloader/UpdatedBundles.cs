using ResonanceTools.Utility;

namespace ResonanceDownloader.Downloader;

public partial class Downloader
{
    public CDNConfig GetPreviousConfig(string previousVersion = "")
    {
        // must be called only after GetConfig() for full necessary data parsing first
        if (string.IsNullOrWhiteSpace(version) || matchedCDNInfos.Count == 0)
        {
            Log.Info("No version specified or cdn info list found, skipping previous version parsing.");
            return new CDNConfig(); 
        }

        CDNConfig prevConfig = new CDNConfig();
        // parse info for specified previous version or just get second lastest version from indexReleaseB
        if (!string.IsNullOrWhiteSpace(previousVersion))
        {
            var prevCDNInfo = matchedCDNInfos
                .FirstOrDefault(x => x.currentVersion == previousVersion);

            if (prevCDNInfo == null)
            {
                Log.Warn($"No config found for {previousVersion}, skipping previous version parsing.");
                return prevConfig;
            }
            
            Log.Info($"Found previous version {previousVersion}");
            prevConfig.baseIndex = prevCDNInfo.baseUrl;
            prevConfig.region = prevCDNInfo.server.ToString();
            prevConfig.platform = prevCDNInfo.platform.ToString();
            prevConfig.version = previousVersion;
        }
        else
        {
            // automatically detect previous version, use base version if no previous version found
            var sorted = matchedCDNInfos
                .OrderByDescending(x =>
                {
                    if (Version.TryParse(x.currentVersion, out var ver))
                        return ver;
                    return new Version(0, 0, 0);
                })
                .ToList();

            // 2. find current version in sorted list
            int currentIndex = sorted.FindIndex(x => x.currentVersion == version);

            CDNInfo? prevCDNInfo = null;

            if (currentIndex == -1) return prevConfig;
            
            if (currentIndex >= 0 && currentIndex < sorted.Count - 1)
            {
                // next one in descending list is the previous version
                prevCDNInfo = sorted[currentIndex + 1];
            }
            else
            {
                prevCDNInfo = sorted[currentIndex];
                // fallback to base version
                prevCDNInfo.currentVersion = prevCDNInfo.baseVersion;
            }           
            if (prevCDNInfo == null)
            {
                Log.Warn($"Unable to determine previous version automatically for {version}.");
                return prevConfig;
            }
            
            Log.Info($"Found previous version {prevCDNInfo.currentVersion}");
            prevConfig.baseIndex = prevCDNInfo.baseUrl;
            prevConfig.region = prevCDNInfo.server.ToString();
            prevConfig.platform = prevCDNInfo.platform.ToString();
            prevConfig.version = prevCDNInfo.currentVersion;
        }
        return prevConfig;
    }

    public void DumpPreviousDescJson(string? previousVersion = null)
    {
        Console.WriteLine($"DumpPreviousDescJson Previous: {previousVersion}");
        CDNConfig prevConfig = GetPreviousConfig(previousVersion);
        if (string.IsNullOrEmpty(prevConfig.version))
        {
            Log.Warn("No previous version info found, skipping description json dump.");
            return;
        }
        
        DumpHotFixBin(prevConfig.GetHotfixBin(), previousVersion);
        prevManifest =  ParseManifest(Path.Combine(outputDir, "metadata", $"{previousVersion}.json"));
    }
}