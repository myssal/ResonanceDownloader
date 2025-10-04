using ResonanceTools.Utility;

namespace ResonanceDownloader.Downloader;

public enum IndexType
{
    
    ReleaseB_CN,
    ReleaseB_GLB,
    ReleaseB_JP,
    ReleaseB_GLB_DBG,
    ReleaseB_CN_DBG1,
    ReleaseB_CN_DBG2,
    ReleaseB_JP_DBG,
    ReleaseB_KR,
    ReleaseB_KR_DBG
}

public enum Platform
{
    Android,
    IOS,
    StandaloneWindows64,
    Harmony
}

public enum Server
{
    EN,
    CN,
    JP,
    KR,
    TW
}

public class CDNConfig
{
    public string baseIndex { get; set; } = "";
    public string region { get; set; } = "";
    public string platform { get; set; } = "";
    public string version { get; set; } = "";

    public CDNConfig(string baseIndex, string region, string platform, string version)
    {
        this.baseIndex = baseIndex;
        this.region = region;
        this.platform = platform;
        this.version = version;
    }

    public CDNConfig()
    {
        
    }
    
    public override string ToString()
    {
        return $"CDNConfig(baseIndex={baseIndex}, region={region}, platform={platform}, version={version})";
    }
    
    public string GetBaseUrl() => $"{baseIndex}/{region}/{platform}/{version}";

    public string GetHotfixBin() => $"{GetBaseUrl()}/desc.bin";
}

public class IndexReleaseInfo
{
    public List<CDNInfo> cdnInfos { get; set; } = new();

    public IndexReleaseInfo(string[] cdnInfoLines)
    {
        foreach (var line in cdnInfoLines)
        {
            cdnInfos.Add(new CDNInfo(line));
        }
    }
}

public class CDNInfo
{
    public Platform platform { get; set; } 
    public Server server { get; set; }
    public string baseVersion { get; set; }
    public string currentVersion { get; set; }
    public string localBaseUrl { get; set; }
    public string baseUrl {get; set;}
    
    /// <summary>
    /// Parse cdn info from a line in index release file.
    /// </summary>
    /// <param name="cfgLine"></param>
    public CDNInfo(string cfgLine)
    {
        // sample: Android,EN,1.5.0,*,1.5.30,1.5.66,https://reso-test.ujoygames.com:443,null,http://resonance-resource.ujoygames.com/,	,	,	false,eNp9Uk1LHEEQXXvf9QH/8...
        
        string[] parts = cfgLine.Split(',');
        if (parts.Length < 9)
        {
            Log.Warn($"Invalid CDN line: {cfgLine}");
            return;
        }
        
        parts = parts.Select(x => x.Trim()).ToArray();
        platform = (Platform)Enum.Parse(typeof(Platform), parts[0], ignoreCase: true);
        server = (Server)Enum.Parse(typeof(Server), parts[1], ignoreCase: true);
        baseVersion = parts[4];
        currentVersion = parts[5];
        localBaseUrl = parts[6];
        baseUrl = parts[8];
    }

    public CDNInfo()
    {
        platform = Platform.StandaloneWindows64;
        server = Server.CN;
        baseVersion = "";
        currentVersion = "";
        localBaseUrl = "";
        baseUrl = "";
    }
}

public static class IndexUrls
{
    private static readonly Dictionary<IndexType, string> UrlMap = new()
    {
        // todo: add TW cdn
        { IndexType.ReleaseB_CN,   "https://eden-index.gameduchy.com/index_ReleaseB.txt" },
        { IndexType.ReleaseB_CN_DBG1, "https://eden-index.gameduchy.com/index_DebugB.txt" },
        { IndexType.ReleaseB_CN_DBG2, "https://eden-index.gameduchy.com/index_Debug.txt" },
        { IndexType.ReleaseB_GLB,  "https://resonance-index.ujoygames.com/index_Release.txt" },
        { IndexType.ReleaseB_GLB_DBG, "https://resonance-index.ujoygames.com/index_Debug.txt" },
        { IndexType.ReleaseB_JP, "https://jp-test-index-rzns.gameduchy.com/index_Release.txt" },
        { IndexType.ReleaseB_JP_DBG, "https://jp-test-index-rzns.gameduchy.com/index_Debug.txt" },
        { IndexType.ReleaseB_KR, "https://reso-index.ujoygames.co.kr/index_Release.txt" },
        { IndexType.ReleaseB_KR_DBG, "https://reso-index.ujoygames.co.kr/index_Debug.txt" }
    };

    public static string GetIndexUrl(IndexType type) => UrlMap[type];
    
    public static string GetIndexUrl(string typeName)
    {
        if (Enum.TryParse<IndexType>(typeName, ignoreCase: true, out var type))
        {
            if (UrlMap.TryGetValue(type, out var url))
                return url;

            throw new ArgumentException($"No url defined for {type}.");
        }

        throw new ArgumentException($"Invalid region type name: {typeName}");
    }
}