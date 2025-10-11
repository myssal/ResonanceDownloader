using Newtonsoft.Json;
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

    public void GenerateDiff(DescManifest ibase, DescManifest current)
    {
        if (ibase == null || current == null)
        {
            Log.Error("Base or current manifest is null, cannot generate diff.");
            return;
        }

        if (ibase.Files == null || current.Files == null)
        {
            Log.Error("Base or current manifest has no files dictionary, cannot generate diff.");
            return;
        }

        // Store updated or new files
        var updatedFiles = new Dictionary<string, FileEntry>();

        foreach (var kv in current.Files)
        {
            string fileKey = kv.Key;
            FileEntry currentEntry = kv.Value;

            if (!ibase.Files.TryGetValue(fileKey, out var baseEntry))
            {
                updatedFiles[fileKey] = currentEntry;
            }
            else
            {
                if (baseEntry.Crc != currentEntry.Crc || baseEntry.Size != currentEntry.Size)
                {
                    updatedFiles[fileKey] = currentEntry;
                }
            }
        }

        Log.Info($"Diff generation complete. Found {updatedFiles.Count} new/updated files.");
        
        string diffJson = JsonConvert.SerializeObject(updatedFiles, Formatting.Indented);
        File.WriteAllText("diff_files.json", diffJson);
    }

}