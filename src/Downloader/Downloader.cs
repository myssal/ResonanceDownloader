using Newtonsoft.Json;
using ResonanceDownloader.Utils;
using ResonanceTools.Utility;
using HotfixParser = ResonanceTools.HotfixParser.Program;
using JabParser = ResonanceTools.JABParser.Program;
using StudioCLI = AssetStudio.CLI.Program;
namespace ResonanceDownloader.Downloader;

public partial class Downloader
{
    private CDNConfig cdnCfg { get; set; }
    private string baseUrl = "";
    private string region = "";
    private string platform = "";
    private string version { get; set; } = "";
    private string prevVersion { get; set; } = "";
    private string outputDir { get; set; } = "";
    private bool downloadCompressedJab { get; set; }
    private List<(string, string)> downloadList {get; set;}
    private DescManifest manifest { get; set; } = new();
    private DescManifest prevManifest { get; set; } = new();
    private PathConfig pathConfig { get; set; } = new(new List<string>(), new List<string>());
    private List<CDNInfo> matchedCDNInfos { get; set; } = new(); 
    
    public Downloader(
        string filterFile,
        bool downloadCompressedJab,
        string? previousVersionCompare = null,
        string? outputDir = null,
        string? version = null, 
        string? presetName = null,
        string? region = null,
        string? platform = null)
    {
        if (string.IsNullOrWhiteSpace(filterFile))
            filterFile = "filters.json";
        
        string indexReleaseBTemp = "";
        this.platform = platform;
        this.region = region;
        
        if (string.IsNullOrEmpty(version))
        {
            Log.Info("No version is provided, fetching version...");
            indexReleaseBTemp = GetConfig(this.region, this.platform);
        }
        else
        {
            Log.Info($"Set version to {version}, fetching config...");
            indexReleaseBTemp = GetConfig(this.region, this.platform, version);
        }
        
        
        // set default folder to outputDir/version right here
        if (string.IsNullOrWhiteSpace(outputDir))
            outputDir = Directory.GetCurrentDirectory();
        
        string verDir = Path.Combine(outputDir, "download", $"{this.region}_{this.platform}",this.version);
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
        
        Log.Info($"Current version: {this.version}");
        if (previousVersionCompare != null)
        {
            prevVersion = previousVersionCompare;
            DumpPreviousDescJson(previousVersionCompare);
        }

        cdnCfg = new CDNConfig(baseUrl, this.region, this.platform, this.version);

        if (!string.IsNullOrEmpty(filterFile))
        {
            string path = filterFile;
            PathPresets pathPresets = new PathPresets();
            pathPresets.Load(path);
            Log.Info($"Loading filter presets from: {path}");
            if (!string.IsNullOrEmpty(presetName))
            {
                Log.Info($"Preset filter loaded: {presetName}");
                pathConfig = pathPresets.Get(presetName);
            }
            else
            {
                Log.Info($"No preset filter match: {path}, load first preset in file...");
                string firstPreset = pathPresets.AvailablePresets.FirstOrDefault() ?? "";
                if (firstPreset != "")
                {
                    Log.Info($"Preset filter loaded: {firstPreset}");
                    pathConfig = pathPresets.Get(firstPreset);
                }
                else
                    Log.Info($"No preset found in {path}, using empty filter.");
            }
        }
        
        this.downloadCompressedJab = downloadCompressedJab;

        LogInfo();
    }

    public void AssetDownload()
    {
        DumpHotFixBin();
        manifest = ParseManifest(Path.Combine(outputDir, "metadata", "desc.json"));
        // downloadList = GetDownloadFileList(pathConfig.Includes, pathConfig.Excludes, downloadCompressedJab);
        // Directories.CreateDownloadSubFolder(downloadList);
        // DownloadAssets(downloadList);
        // JabToBundle();
        // ExtractAsset();
    }
    
    /// <summary>
    /// Log basic info 
    /// </summary>
    private void LogInfo()
    {
        Log.Info("CDN configuration:");
        Log.Info($"Base url: {baseUrl}");
        Log.Info($"Region: {region}");
        Log.Info($"Platform: {platform}");
        Log.Info($"Version: {version}");
    }
    
    /// <summary>
    /// Fetch and dump desc.bin from hotfix server
    /// </summary>
    private void DumpHotFixBin(string hotfixBin = "", string descriptorFileName = "desc")
    {
        DirectoryInfo metadata = new DirectoryInfo(Path.Combine(outputDir, "metadata"));
        metadata.Create();
        
        if(string.IsNullOrEmpty(hotfixBin))
            hotfixBin = cdnCfg.GetHotfixBin();

        Log.Info("Downloading hotfix...");
        Log.Info($"Getting hotfix binary from {hotfixBin}");
        
        string descBinaryPath = Path.Combine(metadata.FullName, $"{descriptorFileName}.bin");
        string descJsonPath = Path.Combine(metadata.FullName, $"{descriptorFileName}.json");
        bool success = HttpRequest.DownloadFile(hotfixBin, descBinaryPath);

        if (success)
        {
            HotfixParser.HotfixWrap(descBinaryPath, descJsonPath);
            File.Delete(descBinaryPath);
        }
        else
        {
            Log.Warn($"Failed to get hotfix binary from {hotfixBin}");
        }
    }
    
    /// <summary>
    /// Return cdn config based on input
    /// </summary>
    /// <param name="currentRegion">Game region to fetch config from</param>
    /// <param name="platform">Game platform</param>
    /// <param name="iversion">Game version</param>
    /// <returns>Temp location of index release file.</returns>
    private string GetConfig(string currentRegion, string platform, string? iversion = null)
    {
        
        string indexUrl = IndexUrls.GetIndexUrl(currentRegion);
        string tempIndexReleaseB = Path.GetTempFileName();
        Platform pplatform = EnumParser.ParsePlatform(platform);
        
        Log.Info($"Downloading index file from {indexUrl}");
        bool success = HttpRequest.DownloadFile(indexUrl, tempIndexReleaseB);
        
        if (success)
        {
            CDNInfo? matchedCdnInfo = null;
            Log.Info($"Processing cdn info...");
            var cdnContent = File.ReadAllLines(tempIndexReleaseB);
            IndexReleaseInfo idxRelease = new IndexReleaseInfo(cdnContent);
            
            // filter index that matches the platform
            var matchedCdn = idxRelease.cdnInfos.Where(x => x.platform == pplatform);
            
            if (matchedCdn.Count() == 0)
            {
                Log.Warn($"No matching platform found for {platform}, use default platform PC.");
                matchedCdn = idxRelease.cdnInfos.Where(x => x.platform == Platform.StandaloneWindows64);
                
            }
            
            // save matched platform cdn info for modification filter
            matchedCDNInfos = matchedCdn.ToList();
            // filter based on version if provided else get highest version of the platform
            if (!string.IsNullOrEmpty(iversion))
            {
                var versionedCdn = matchedCdn.Where(x => x.currentVersion == iversion).FirstOrDefault();
                if (versionedCdn != null)
                {
                    Log.Info($"Found matching cdn config for provided version {iversion}");
                    matchedCdnInfo = versionedCdn;
                }
                else
                {
                    Log.Info($"No matching cdn config for provided version {iversion}, using lastest version...");
                    matchedCdnInfo = GetLastestVersion(matchedCdn.ToList());
                }
            }
            else
            {
                matchedCdnInfo = GetLastestVersion(matchedCdn.ToList());
            }
            
            if (matchedCdnInfo != null)
            {
                Log.Info("Cdn info found:");
                baseUrl = matchedCdnInfo.baseUrl;
                region = matchedCdnInfo.server.ToString();
                this.platform = platform;
                version = matchedCdnInfo.currentVersion;
            }
            else
            {
                Log.Warn($"No matching cdn info found for platform {platform} and version {iversion}");
            }
        }
        return tempIndexReleaseB;
    }

    public CDNInfo GetLastestVersion(List<CDNInfo> cdnInfos)
    {
        CDNInfo matchedCdnInfo = new CDNInfo();
        // pick highest version
        matchedCdnInfo = cdnInfos
            .Where(x => EnumParser.ParseVersion(x.currentVersion, out _))   // only valid versions
            .OrderByDescending(x =>
            {
                EnumParser.ParseVersion(x.currentVersion, out var v);
                return v;
            })
            .FirstOrDefault();
        return matchedCdnInfo;
    }
    
    /// <summary>
    /// Parsing desc.json to get manifest
    /// </summary>
    /// <param name="manifestJson">Path to dumped desc.json</param>
    private DescManifest ParseManifest(string manifestJson)
    {
        if (!File.Exists(manifestJson))
        {
            Log.Warn($"Manifest file {manifestJson} not found.");
            return new DescManifest();
        }  
        string content = File.ReadAllText(manifestJson);
        
        var dumpManifest = JsonConvert.DeserializeObject<DescManifest>(content);
        Log.Info($"Manifest parsed, total files: {dumpManifest.Files.Count}");
        return dumpManifest;
    }
    /// <summary>
    /// Get assets list to download, based on exclude and include filter. Compressed jabs are not included by default.
    /// </summary>
    /// <param name="includes">only filter asset contains these keywords</param>
    /// <param name="excludes">only filter asset not contains these keywords</param>
    /// <param name="downloadCompressedJab">download compressed jab files or not</param>
    /// <returns>a list of download url and local relative path</returns>
    private List<(string, string)> GetDownloadFileList(List<string> includes, List<string> excludes, bool 
        downloadCompressedJab)
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
    private void DownloadAssets(List<(string, string)> downloadList)
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
    private void JabToBundle(string jabFolder = "", string extractOutput = "", string bufferSize = "")
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
    /// <param name="keyIndex">unity cn key index in asset studio keys.json file</param>
    private void ExtractAsset(string inputPath = "", string outputPath = "", List<string>? types = null, 
        AssetGroupOption option = AssetGroupOption.ByContainer, 
        int keyIndex = 0)
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
    
    private enum AssetGroupOption
    {
        ByType,
        ByContainer,
        BySource,
        None
    }
}

