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
    
    /// <summary>
    /// Clears all files and subfolders in the specified directory.
    /// </summary>
    /// <param name="path">The directory to clear.</param>
    public static void Clear(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
        
        foreach (string file in Directory.GetFiles(path))
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }
        
        foreach (string dir in Directory.GetDirectories(path))
        {
            Directory.Delete(dir, true); 
        }
    }
}