using ResonanceTools.Utility;

namespace ResonanceDownloader.Downloader;

public partial class Downloader
{
    public void DumpBaseDescJson()
    {
        // just use base version dude
        try
        {
            CDNConfig baseConfig = cdnCfg;
            baseConfig.version = cdnInfo.baseVersion;
            if (string.IsNullOrEmpty(baseConfig.version))
            {
                Log.Warn("No base version info found, skipping base description json dump.");
                return;
            }

            DumpHotFixBin(baseConfig.GetHotfixBin(), "base_desc");
            string descJson = Path.Combine(outputDir, "metadata", $"base_desc.json");
            prevManifest = ParseManifest(descJson);
        }
        catch (Exception ex)
        {
            Log.Error($"DumpBaseDescJson failed: {ex}");
        }
    }

}