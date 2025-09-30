using ResonanceDownloader.Utils;
using ResonanceTools.Utility;
using static ResonanceTools.HotfixParser.Program;
namespace ResonanceDownloader.Downloader;

public class Downloader
{
    private const string indexReleaseB = "https://eden-index.gameduchy.com/index_ReleaseB.txt";
    private string baseUrl   = "http://eden-resource-volc.gameduchy.com";
    private string region    = "CN";
    private string platform  = "StandaloneWindows64";
    private CDNConfig cdnCfg { get; set; }
    private string outputDir { get; set; }
    private string version { get; set; }
    
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
        
        DumpHotFixBin();
    }

    public void LogInfo()
    {
        Log.Info("Fetching CDN configuration...");
        Log.Info($"Base url: {baseUrl}");
        Log.Info($"Region: {region}");
        Log.Info($"Platform: {platform}");
        Log.Info($"Version: {version}");
    }
    
    public void DumpHotFixBin()
    {
        DirectoryInfo metadata = new DirectoryInfo(Path.Combine(outputDir, "metadata"));
        metadata.Create();
        string hotfixBin = cdnCfg.GetHotfixBin();
        
        Log.Info("Downloading hotfix...");
        Log.Info($"Getting hotfix binary from {hotfixBin}");
        
        bool sucess = HttpRequest.DownloadFile(hotfixBin, $"{metadata.FullName}/desc.bin");
        
        if (sucess)
            HotfixWrap($"{metadata.FullName}/desc.bin", $"{metadata.FullName}/desc.json");
        else
        {
            Log.Warn($"Failed to get hotfix binary from {hotfixBin}");
        }
    }
    
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
}