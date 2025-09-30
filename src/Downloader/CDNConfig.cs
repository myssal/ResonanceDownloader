namespace ResonanceDownloader.Downloader;

public class CDNConfig
{
    public string baseIndex { get; set; }
    public string region { get; set; }
    public string platform { get; set; }
    public string version { get; set; }

    public CDNConfig(string baseIndex, string region, string platform, string version)
    {
        this.baseIndex = baseIndex;
        this.region = region;
        this.platform = platform;
        this.version = version;
    }
    
    public override string ToString()
    {
        return $"CDNConfig(baseIndex={baseIndex}, region={region}, platform={platform}, version={version})";
    }
    
    public string GetBaseUrl() => $"{baseIndex}/{region}/{platform}/{version}";

    public string GetHotfixBin() => $"{GetBaseUrl()}/desc.bin";

    


}