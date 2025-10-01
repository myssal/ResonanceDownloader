using Newtonsoft.Json;
using ResonanceDownloader.Utils;
using ResonanceTools.Utility;
using HotfixParser = ResonanceTools.HotfixParser.Program;
using JabParser = ResonanceTools.JABParser.Program;
using StudioCLI = AssetStudio.CLI.Program;
namespace ResonanceDownloader.Downloader;

public class Downloader
{
    private const string indexReleaseB = "https://eden-index.gameduchy.com/index_ReleaseB.txt";
    private string baseUrl = "http://eden-resource-volc.gameduchy.com";
    private string region = "CN";
    private string platform = "StandaloneWindows64";
    
    private CDNConfig cdnCfg { get; set; }
    private string outputDir { get; set; }
    private string version { get; set; }
    private bool downloadCompressedJab { get; set; }
    private List<(string, string)> downloadList {get; set;}
    private DescManifest manifest { get; set; } = new();
    private PathConfig pathConfig { get; set; } = new();
    
    public Downloader(string filterFile, bool downloadCompressedJab, string outputDir, string? version = null, string? presetName = null)
    {
        if (string.IsNullOrWhiteSpace(filterFile))
            filterFile = "filters.json";
        
        string indexReleaseBTemp = "";
        if (string.IsNullOrEmpty(version))
        {
            Log.Info("No version is provided, fetching version...");
            (this.version, indexReleaseBTemp) = GetVersion();
        }
        else
        {
            this.version = version;
        }
        
        // set default folder to outputDir/version right here
        string verDir = Path.Combine(outputDir, this.version);
        DirectoryInfo directoryInfo = new DirectoryInfo(verDir);
        directoryInfo.Create();
        this.outputDir = verDir;
        Directories.Clear(this.outputDir);
        if (!string.IsNullOrEmpty(indexReleaseBTemp))
        {
            string metadataPath = Path.Combine(this.outputDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            File.Move(indexReleaseBTemp, $"{metadataPath}/index_ReleaseB.txt", true);
        }
            

        cdnCfg = new CDNConfig(baseUrl, region, platform, this.version);

        if (!string.IsNullOrEmpty(filterFile))
        {
            string path = filterFile;
            PathPresets pathPresets = new PathPresets();
            pathPresets.Load(path);
            Log.Info($"Loaded filter presets from: {path}");
            if (!string.IsNullOrEmpty(presetName))
            {
                Log.Info($"Loaded preset filter: {presetName}");
                pathConfig = pathPresets.Get(presetName);
            }
            else
            {
                Log.Info($"No preset filter match: {path}, load first preset in file...");
                string firstPreset = pathPresets.AvailablePresets.FirstOrDefault() ?? "";
                Log.Info($"Using {firstPreset}");
                pathConfig = pathPresets.Get(firstPreset);
            }
        }
        
        this.downloadCompressedJab = downloadCompressedJab;

        LogInfo();
    }

    public void AssetDownload()
    {
        DumpHotFixBin();
        ParseManifest(Path.Combine(outputDir, "metadata", "desc.json"));
        downloadList = GetDownloadFileList(pathConfig.Includes, pathConfig.Excludes, downloadCompressedJab);
        Directories.CreateDownloadSubFolder(downloadList);
        DownloadAssets(downloadList);
        JabToBundle();
        ExtractAsset();
    }
    
    /// <summary>
    /// Log basic info 
    /// </summary>
    public void LogInfo()
    {
        Log.Info("CDN configuration");
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
        {
            HotfixParser.HotfixWrap($"{metadata.FullName}/desc.bin", $"{metadata.FullName}/desc.json");
            File.Delete($"{metadata.FullName}/desc.bin");
        }
            
        else
        {
            Log.Warn($"Failed to get hotfix binary from {hotfixBin}");
        }
    }
    
    /// <summary>
    /// Get current game version
    /// </summary>
    /// <returns>game version & temp indexReleaseB path</returns>
    public (string, string) GetVersion()
    {
        string indexUrl = indexReleaseB;
        string tempIndexReleaseB = Path.GetTempFileName();
        string version = "";

        Log.Info($"Downloading index file from {indexUrl}");

        bool sucess = HttpRequest.DownloadFile(indexUrl, tempIndexReleaseB);

        if (sucess)
        {
            string content = File.ReadAllText(tempIndexReleaseB);
            string[] parts = content.Split(',');
            version = parts[5].Trim();
        }
        return (version, tempIndexReleaseB);
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
    /// Get assets list to download, based on exclude and include filter. Compressjabs are not included by default.
    /// </summary>
    /// <param name="includes">only filter asset contains these keywords</param>
    /// <param name="excludes">only filter asset not contains these keywords</param>
    /// <param name="downloadCompressedJab">download compressed jab files or not</param>
    /// <returns>a list of download url and local relative path</returns>
    public List<(string, string)> GetDownloadFileList(List<string> includes, List<string> excludes, bool downloadCompressedJab)
    {
        List<(string, string)> downloadList = new();
        DirectoryInfo rawDir = new DirectoryInfo(Path.Combine(outputDir, "bundles"));
        rawDir.Create();

        if (manifest.CompressedJabNames.Count == 0 && manifest.Files.Count == 0)
            Log.Warn("Manifest is empty, please parse the manifest first.");
        else
        {
            if (downloadCompressedJab)
            {
                Log.Info("Adding compressed jab entries...");
                foreach (var compressedJab in manifest.CompressedJabNames)
                {
                    string url = $"{cdnCfg.GetBaseUrl()}/{compressedJab}";
                    string localPath = Path.Combine(rawDir.FullName, "jab", compressedJab);
                    downloadList.Add((url, localPath));
                }
            }
            else
                Log.Info("Skipping download compressed jabs.");

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
                    string localPath = Path.Combine(rawDir.FullName, "raw", path);
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
        string expectJabFolder = Path.Combine(outputDir, "bundles", "jab");
        
        if (string.IsNullOrEmpty(jabFolder))
        {
            
            if (!Directory.Exists(expectJabFolder))
            {
                Log.Warn($"JAB folder not specified and default path {expectJabFolder} does not exist.");
                return;
            }
            
            jabFolder = expectJabFolder;
        }
        
        DirectoryInfo rawExtract = new DirectoryInfo(Path.Combine(outputDir, "bundles", "raw"));
        if (string.IsNullOrEmpty(extractOutput))
        {
            rawExtract.Create();
            extractOutput = rawExtract.FullName;
        }
            
        string[] args = new[]
        {
            jabFolder,
            "--extract", extractOutput,
            "--buffer", string.IsNullOrEmpty(bufferSize) ? "262144" : bufferSize
        };
        
        JabParser.JabParseWrap(args);
    }
    
    /// <summary>
    /// Extract raw bundles
    /// </summary>
    /// <param name="inputPath">specify raw bundles path</param>
    /// <param name="outputPath">specify extract assets path</param>
    /// <param name="types">specify asset types to export</param>
    /// <param name="option">specify assets group option: ByType, ByContainer, BySource or None.</param>
    /// <param name="keyIndex">unitycn key index in asset studio keys.json file</param>
    public void ExtractAsset(string inputPath = "", string outputPath = "", List<string>? types = null, AssetGroupOption option = AssetGroupOption.ByContainer, 
        int keyIndex = 14)
    {
        // set default values
        if (string.IsNullOrEmpty(inputPath)) inputPath = Path.Combine(outputDir, "bundles", "raw");
        if (string.IsNullOrEmpty(outputPath)) outputPath = Path.Combine(outputDir, "extracted");
        types ??= new List<string>
        {
            "TextAsset",
            "Texture2D",
            "VideoClip"
        };

        var args = new List<string>
        {
            inputPath,
            outputPath,
            "--game", "UnityCN",
            "--key_index", keyIndex.ToString(),
            "--group_assets", option.ToString() 
        };
        
        if (types.Count > 0)
        {
            args.Add("--types");
            args.AddRange(types);
        }
        
        StudioCLI.Main(args.ToArray());
    }
    
    public enum AssetGroupOption
    {
        ByType,
        ByContainer,
        BySource,
        None
    }
}

