using ResonanceTools.Utility;

namespace ResonanceDownloader.Utils;

public class Directories
{
    /// <summary>
    /// Create needed subfolder from a list of downloadList.localPath
    /// </summary>
    /// <param name="downloadList"></param>
    public static void CreateDownloadSubFolder(List<(string url, string localPath)> downloadList)
    {
        Log.Info("Creating download sub folders...");
        HashSet<string> createdDirs = new(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, localPath) in downloadList)
        {
            var dir = Path.GetDirectoryName(localPath); 
            if (string.IsNullOrEmpty(dir) || createdDirs.Contains(dir)) continue;
            Log.Info($"Creating directory: {dir}");
            Directory.CreateDirectory(dir);
            createdDirs.Add(dir);
        }
    }
}