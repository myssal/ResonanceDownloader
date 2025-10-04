using ResonanceTools.Utility;

namespace ResonanceDownloader.Downloader;

public partial class Downloader
{
    public CDNConfig GetPreviousConfig(string previousVersion = "")
    {
        // just use base version bruh
        // must be called only after GetConfig() for full necessary data parsing first
        if (string.IsNullOrWhiteSpace(version) || matchedCDNInfos.Count == 0)
        {
            Log.Error("GetPreviousConfig() called before GetConfig() or version not set. Skipping previous version parsing.");
            return CDNConfig.Invalid("Missing version or empty CDN list");
        }

        CDNConfig prevConfig = new CDNConfig();

        if (!string.IsNullOrWhiteSpace(previousVersion))
        {
            var prevCDNInfos = matchedCDNInfos
                .FirstOrDefault(x => x.currentVersion == previousVersion);

            if (prevCDNInfos == null)
            {
                Log.Warn($"No config found for {previousVersion}, skipping previous version parsing.");
                return CDNConfig.Invalid($"No config found for version {previousVersion}");
            }

            Log.Info($"Found previous version {previousVersion}");
            return FromCDNInfo(prevCDNInfos);
        }

        var validInfos = matchedCDNInfos
            .Where(x => !string.IsNullOrWhiteSpace(x.currentVersion) &&
                        Version.TryParse(x.currentVersion, out _))
            .ToList();

        if (validInfos.Count == 0)
        {
            Log.Warn("No valid version strings found in CDN info list.");
            return CDNConfig.Invalid("No valid versions in CDN info list");
        }

        var sorted = validInfos
            .OrderByDescending(x => Version.Parse(x.currentVersion))
            .ToList();

        int currentIndex = sorted.FindIndex(x => x.currentVersion == version);
        if (currentIndex == -1)
        {
            Log.Warn($"Current version {version} not found in CDN list.");
            return CDNConfig.Invalid($"Version {version} not in CDN list");
        }


        CDNInfo? prevCDNInfo = null;
        if (currentIndex >= 0 && currentIndex < sorted.Count - 1)
        {
            prevCDNInfo = sorted[currentIndex + 1];
        }
        else
        {
            prevCDNInfo = sorted[currentIndex];
            prevCDNInfo.currentVersion = prevCDNInfo.baseVersion;
        }

        if (string.IsNullOrEmpty(prevCDNInfo.baseUrl))
            Log.Warn($"Previous CDN info for {prevCDNInfo.currentVersion} has no baseUrl.");

        Log.Info($"Found previous version {prevCDNInfo.currentVersion}");
        return FromCDNInfo(prevCDNInfo);
    }

    public static CDNConfig FromCDNInfo(CDNInfo info)
    {
        return new CDNConfig
        {
            baseIndex = info.baseUrl,
            region = info.server.ToString(),
            platform = info.platform.ToString(),
            version = info.currentVersion
        };
    }


    public void DumpPreviousDescJson(string? ipreviousVersion = null)
    {
        
        CDNConfig prevConfig = GetPreviousConfig(ipreviousVersion);
        Console.WriteLine($"PREVIOUS: {prevConfig.version}");
        if (string.IsNullOrEmpty(prevConfig.version))   
        {
            Log.Warn("No previous version info found, skipping description json dump.");
            return;
        }
        
        DumpHotFixBin(prevConfig.GetHotfixBin(), ipreviousVersion);
        string descJson = Path.Combine(outputDir, "metadata", $"{ipreviousVersion}.json");
        Log.Info($"JSOOOOOON: {descJson}");
        prevManifest =  ParseManifest(descJson);
    }
}