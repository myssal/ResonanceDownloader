using Newtonsoft.Json;
using ResonanceDownloader.Utils;
using ResonanceTools.Utility;
using HotfixParser = ResonanceTools.HotfixParser.Program;
using JabParser = ResonanceTools.JABParser.Program;
namespace ResonanceDownloader.Downloader;

public class Downloader
{
    private const string indexReleaseB = "https://eden-index.gameduchy.com/index_ReleaseB.txt";
    private string baseUrl = "http://eden-resource-volc.gameduchy.com";
    private string region = "CN";
    private string platform = "StandaloneWindows64";
    private List<string> includes = new List<string>
    {
        "/enemy/",
        "/enemyplus/",
        "/environment/env_ui",
        "/environment/rn_scenes",
        "/home/",
        "/item/",
        "/kabaneri/",
        "/npc/",
        "/originpack/",
        "/role/",
        "/roleplus/",
        "/spine/",
        "/story/",
        "/texticon/",
        "/timeline/",
        "/ui/",
        "/video/",
        "/weather/"
    };
    private List<string> excludes = new List<string>
    {
        "3d",
        "model",
        "commoneffect"
    };
    
    
    private CDNConfig cdnCfg { get; set; }
    private string outputDir { get; set; }
    private string version { get; set; }
    private List<(string, string)> downloadList {get; set;}
    private DescManifest manifest { get; set; } = new();

    public Downloader(string version, string outputDir)
    {
        this.version = version;
        cdnCfg = new CDNConfig(baseUrl, region, platform, version);
        DirectoryInfo directoryInfo = new DirectoryInfo(outputDir);
        directoryInfo.Create();
        this.outputDir = outputDir;

        LogInfo();

    }

    public Downloader(string outputDir)
    {
        Log.Info("No version is provided, fetching version...");

        DirectoryInfo directoryInfo = new DirectoryInfo(outputDir);
        directoryInfo.Create();
        this.outputDir = outputDir;
        version = GetVersion();
        cdnCfg = new CDNConfig(baseUrl, region, platform, version);

        LogInfo();
    }

    public void AssetDownload()
    {
        DumpHotFixBin();
        ParseManifest(Path.Combine(outputDir, "metadata", "desc.json"));
        downloadList = GetDownloadFileList(includes, excludes);
        Directories.CreateDownloadSubFolder(downloadList);
        DownloadAssets(downloadList);
        JabToBundle();
    }
    
    /// <summary>
    /// Log basic info 
    /// </summary>
    public void LogInfo()
    {
        Log.Info("Fetching CDN configuration...");
        Log.Info($"Base url: {baseUrl}");
        Log.Info($"Region: {region}");
        Log.Info($"Platform: {platform}");
        Log.Info($"Version: {version}");
    }
    
    /// <summary>
    /// Fetch and dump desc.bin from hotfix server
    /// </summary>
    public void DumpHotFixBin()
    {
        DirectoryInfo metadata = new DirectoryInfo(Path.Combine(outputDir, "metadata"));
        metadata.Create();
        string hotfixBin = cdnCfg.GetHotfixBin();

        Log.Info("Downloading hotfix...");
        Log.Info($"Getting hotfix binary from {hotfixBin}");

        bool sucess = HttpRequest.DownloadFile(hotfixBin, $"{metadata.FullName}/desc.bin");

        if (sucess)
            HotfixParser.HotfixWrap($"{metadata.FullName}/desc.bin", $"{metadata.FullName}/desc.json");
        else
        {
            Log.Warn($"Failed to get hotfix binary from {hotfixBin}");
        }
    }
    
    /// <summary>
    /// Fetching game version
    /// </summary>
    /// <returns>patch version</returns>
    public string GetVersion()
    {
        string indexUrl = indexReleaseB;
        DirectoryInfo metadata = new DirectoryInfo(Path.Combine(outputDir, "metadata"));
        metadata.Create();
        string indexFilePath = Path.Combine(metadata.FullName, "index_ReleaseB.txt");
        string version = "";

        Console.WriteLine($"Downloading index file from {indexUrl}");

        bool sucess = HttpRequest.DownloadFile(indexUrl, indexFilePath);

        if (sucess)
        {
            string content = File.ReadAllText(indexFilePath);
            string[] parts = content.Split(',');
            version = parts[5].Trim();
        }

        File.Delete(indexFilePath);
        return version;
    }
    
    /// <summary>
    /// Parsing desc.json to get manifest
    /// </summary>
    /// <param name="manifestJson">Path to dumped desc.json</param>
    public void ParseManifest(string manifestJson)
    {
        if (!File.Exists(manifestJson))
            Log.Warn($"Manifest file {manifestJson} not found.");
        else
        {
            string content = File.ReadAllText(manifestJson);
            manifest = JsonConvert.DeserializeObject<DescManifest>(content);
            Log.Info($"Manifest parsed, total files: {manifest.Files.Count}");
        }
    }
    /// <summary>
    /// Get assets list to download, based on exclude and include filter. Compressjabs are included by default.
    /// </summary>
    /// <param name="includes">only filter asset contains these keywords</param>
    /// <param name="excludes">only filter asset not contains these keywords</param>
    /// <returns>a list of download url and local relative path</returns>
    public List<(string, string)> GetDownloadFileList(List<string> includes, List<string> excludes)
    {
        List<(string, string)> downloadList = new();
        DirectoryInfo rawDir = new DirectoryInfo(Path.Combine(outputDir, version));
        rawDir.Create();

        if (manifest.CompressedJabNames.Count == 0 && manifest.Files.Count == 0)
            Log.Warn("Manifest is empty, please parse the manifest first.");
        else
        {
            Log.Info("Adding compressed jab entries...");
            foreach (var compressedJab in manifest.CompressedJabNames)
            {
                string url = $"{cdnCfg.GetBaseUrl()}/{compressedJab}";
                string localPath = Path.Combine(rawDir.FullName, "jab", compressedJab);
                downloadList.Add((url, localPath));
            }

            Log.Info("Filter asset list...");
            int assetCount = 0;
            foreach (var fileEntry in manifest.Files)
            {
                var entry = fileEntry.Value;
                string path = entry.Path;
                
                bool include = includes == null || includes.Count == 0 || includes.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));
                bool exclude = excludes != null && excludes.Any(x => path.Contains(x, StringComparison.OrdinalIgnoreCase));

                if (include && !exclude)
                {
                    string url = $"{cdnCfg.GetBaseUrl()}/{path}";
                    string localPath = Path.Combine(rawDir.FullName, path);
                    downloadList.Add((url, localPath));
                    assetCount++;
                }
            }
            Log.Info($"Found {assetCount} assets match the filters.");
        }
        
        return downloadList;
    }
    
    /// <summary>
    /// Download assets from given download list
    /// </summary>
    /// <param name="downloadList"></param>
    public void DownloadAssets(List<(string, string)> downloadList)
    {
        Log.Info($"Downloading {downloadList.Count} assets...");
        var failedDownloads = HttpRequest.DownloadFilesParallel(downloadList, 4, 3);

        string progressFilePath = Path.Combine(outputDir, "metadata", "progress.txt");
        using (StreamWriter writer = new StreamWriter(progressFilePath))
        {
            foreach (var (url, filePath) in downloadList)
            {
                string status = failedDownloads.Contains(url) ? "failed" : "downloaded";
                writer.WriteLine($"{filePath}: {status}");
            }
        }

        Log.Info($"Download progress saved to {progressFilePath}");
    }

    /// <summary>
    /// Parse asset bundle from jab files
    /// </summary>
    /// <param name="jabFolder">Jab folder path</param>
    /// <param name="extractOutput">Extracted asset bundles output path</param>
    /// <param name="bufferSize"></param>
    public void JabToBundle(string jabFolder = "", string extractOutput = "", string bufferSize = "")
    {
        string expectJabFolder = Path.Combine(outputDir, version, "jab");
        DirectoryInfo tempExtract = new DirectoryInfo(Path.Combine(outputDir, version, "Data"));
        if (string.IsNullOrEmpty(jabFolder))
        {
            
            if (!Directory.Exists(expectJabFolder))
            {
                Log.Warn($"JAB folder not specified and default path {expectJabFolder} does not exist.");
                return;
            }
            
            jabFolder = expectJabFolder;
        }

        if (string.IsNullOrEmpty(extractOutput))
        {
            tempExtract.Create();
            extractOutput = tempExtract.FullName;
        }
            
        string[] args = new[]
        {
            jabFolder,
            "--extract", extractOutput,
            "--buffer", string.IsNullOrEmpty(bufferSize) ? "262144" : bufferSize
        };
        
        JabParser.JabParseWrap(args);
        
        
    }
}

