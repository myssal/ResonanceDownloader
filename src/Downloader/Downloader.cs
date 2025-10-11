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
    private CDNInfo cdnInfo { get; set; } = new();
    private bool compareToBase { get; set; } = false;
    private string outputDir { get; set; } = "";
    private bool downloadCompressedJab { get; set; }
    private List<(string, string)> downloadList {get; set;}
    private DescManifest manifest { get; set; } = new();
    private DescManifest prevManifest { get; set; } = new();
    private PathConfig pathConfig { get; set; } = new(new List<string>(), new List<string>());
    private List<CDNInfo> matchedCDNInfos { get; set; } = new(); 
    
    private ArgInput argInput { get; set; }
    
    public Downloader(
        string filterFile,
        bool downloadCompressedJab,
        bool compareToBase = false,
        string? outputDir = null,
        string? version = null, 
        string? presetName = null,
        string? region = null,
        string? platform = null)
    {
        argInput = new ArgInput(filterFile, downloadCompressedJab, compareToBase, outputDir, version, presetName, region, platform);
    }

    public async Task InitializeMetadata()
    {
        if (string.IsNullOrWhiteSpace(argInput.filterFile))
            argInput.filterFile = "filters.json";
        
        string indexReleaseBTemp = "";
        
        if (string.IsNullOrEmpty(argInput.version))
        {
            Log.Info("No version is provided, fetching version...");
            indexReleaseBTemp = await GetConfig(argInput.region, argInput.platform);
        }
        else
        {
            Log.Info($"Set version to {argInput.version}, fetching config...");
            indexReleaseBTemp = await GetConfig(argInput.region, argInput.platform, argInput.version);
        }
            
        
        // set default folder to outputDir/version right here
        if (string.IsNullOrWhiteSpace(outputDir))
            outputDir = Directory.GetCurrentDirectory();
        
        string verDir = Path.Combine(outputDir, "download", $"{argInput.region}_{argInput.platform}",cdnInfo.currentVersion);
        DirectoryInfo directoryInfo = new DirectoryInfo(verDir);
        directoryInfo.Create();
        outputDir = verDir;
        compareToBase = argInput.compareToBase;
        Directories.Clear(this.outputDir);
        if (!string.IsNullOrEmpty(indexReleaseBTemp))
        {
            string metadataPath = Path.Combine(this.outputDir, "metadata");
            Directory.CreateDirectory(metadataPath);
            File.Move(indexReleaseBTemp, $"{metadataPath}/index_ReleaseB.txt", true);
        }
        
        Log.Info($"Current version: {cdnInfo.currentVersion}");
        

        cdnCfg = new CDNConfig(
            cdnInfo.baseUrl,
            cdnInfo.server.ToString(),
            cdnInfo.platform.ToString(), 
            cdnInfo.currentVersion);
        
        
        
        if (!string.IsNullOrEmpty(argInput.filterFile))
        {
            string path = argInput.filterFile;
            PathPresets pathPresets = new PathPresets();
            pathPresets.Load(path);
            Log.Info($"Loading filter presets from: {path}");
            if (!string.IsNullOrEmpty(argInput.presetName))
            {
                Log.Info($"Preset filter loaded: {argInput.presetName}");
                pathConfig = pathPresets.Get(argInput.presetName);
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
        
        downloadCompressedJab = argInput.downloadCompressedJab;

        LogInfo();
    }
    
    public async Task AssetDownload()
    {
        await DumpHotFixBin();
        manifest = ParseManifest(Path.Combine(outputDir, "metadata", "desc.json"));
        if (compareToBase)
        {
            Log.Info($"Dumping base desc binary...");
            DumpBaseDescJson();
            GenerateDiff(prevManifest, manifest);
        }
        downloadList = GetDownloadFileList(pathConfig.Includes, pathConfig.Excludes, downloadCompressedJab);
        Directories.CreateDownloadSubFolder(downloadList);
        await DownloadAssetsAsync(downloadList);
        JabToBundle();
        ExtractAsset();
    }
    
    /// <summary>
    /// Log basic info 
    /// </summary>
    private void LogInfo()
    {
        Log.Info("CDN configuration:");
        Log.Info($"Base url: {cdnInfo.baseUrl}");
        Log.Info($"Region: {cdnInfo.server.ToString()}");
        Log.Info($"Platform: {cdnInfo.platform.ToString()}");
        Log.Info($"Version: {cdnInfo.currentVersion}");
    }
    
    /// <summary>
    /// Fetch and dump desc.bin from hotfix server
    /// </summary>
    private async Task DumpHotFixBin(string hotfixBin = "", string descriptorFileName = "desc")
    {
        DirectoryInfo metadata = new DirectoryInfo(Path.Combine(outputDir, "metadata"));
        metadata.Create();
        
        if(string.IsNullOrEmpty(hotfixBin))
            hotfixBin = cdnCfg.GetHotfixBin();

        Log.Info("Downloading hotfix...");
        Log.Info($"Getting hotfix binary from {hotfixBin}");
        
        string descBinaryPath = Path.Combine(metadata.FullName, $"{descriptorFileName}.bin");
        string descJsonPath = Path.Combine(metadata.FullName, $"{descriptorFileName}.json");
        bool success = await DownloadRequest.DownloadFileAsync(hotfixBin, descBinaryPath);

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
    private async Task<string> GetConfig(string currentRegion, string platform, string? iversion = null)
    {
        
        string indexUrl = IndexUrls.GetIndexUrl(currentRegion);
        string tempIndexReleaseB = Path.GetTempFileName();
        Platform pplatform = EnumParser.ParsePlatform(platform);
        
        Log.Info($"Downloading index file from {indexUrl}");
        bool success = await DownloadRequest.DownloadFileAsync(indexUrl, tempIndexReleaseB);
        
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
                cdnInfo.baseUrl = matchedCdnInfo.baseUrl;
                cdnInfo.server = matchedCdnInfo.server;
                cdnInfo.platform = EnumParser.ParsePlatform(platform);
                cdnInfo.currentVersion = matchedCdnInfo.currentVersion;
                cdnInfo.baseVersion = matchedCdnInfo.baseVersion;
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
    private async Task DownloadAssetsAsync(List<(string url, string filePath)> downloadList)
    {
        Log.Info($"Downloading {downloadList.Count} assets...");

        // Run all downloads in parallel using the new DownloadRequest class
        var failedDownloads = await DownloadRequest.DownloadFilesParallelAsync(
            downloadList,
            maxDegreeOfParallelism: 4,
            maxRetries: 3
        );

        // Write progress file
        string progressFilePath = Path.Combine(outputDir, "metadata", "progress.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(progressFilePath)!);

        await using (var writer = new StreamWriter(progressFilePath, false))
        {
            foreach (var (url, filePath) in downloadList)
            {
                string status = failedDownloads.Contains(url) ? "failed" : "downloaded";
                await writer.WriteLineAsync($"{filePath}: {status}");
            }
        }

        Log.Info($"Download complete. Success: {downloadList.Count - failedDownloads.Count}, Failed: {failedDownloads.Count}");
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

public class ArgInput
{
    public string filterFile { get; set; } 
    public string? version { get; set; }
    public string? presetName { get; set; }
    public string? region { get; set; }
    public string? platform { get; set; }
    public string outputDir { get; set; } 
    public bool downloadCompressedJab { get; set; } 
    public bool compareToBase { get; set; } 
    
    public ArgInput(
        string filterFile,
        bool downloadCompressedJab,
        bool compareToBase = false,
        string? outputDir = null,
        string? version = null,
        string? presetName = null,
        string? region = null,
        string? platform = null)
    {
        this.filterFile = filterFile;
        this.downloadCompressedJab = downloadCompressedJab;
        this.compareToBase = compareToBase;
        this.outputDir = outputDir;
        this.version = version;
        this.presetName = presetName;
        this.region = region;
        this.platform = platform;
    }
}

