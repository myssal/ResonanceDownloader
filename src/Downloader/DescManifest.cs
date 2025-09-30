using Newtonsoft.Json;

namespace ResonanceDownloader.Downloader;

public class DescManifest
{
    [JsonProperty("date")]
    public string Date { get; set; }

    [JsonProperty("patchVersion")]
    public string PatchVersion { get; set; }

    [JsonProperty("baseVersion")]
    public string BaseVersion { get; set; }

    [JsonProperty("overrideDic")]
    public OverrideDic OverrideDic { get; set; }

    [JsonProperty("header")]
    public object Header { get; set; }

    [JsonProperty("compressedJabNames")]
    public List<string> CompressedJabNames { get; set; }

    [JsonProperty("files")]
    public Dictionary<string, FileEntry> Files { get; set; }
}

public class OverrideDic
{
    [JsonProperty("Properties")]
    public Dictionary<string, string> Properties { get; set; }
}

public class FileEntry
{
    [JsonProperty("jabName", NullValueHandling = NullValueHandling.Ignore)]
    public string JabName { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("compress", NullValueHandling = NullValueHandling.Ignore)]
    public bool? Compress { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("uncSize", NullValueHandling = NullValueHandling.Ignore)]
    public long? UncSize { get; set; }

    [JsonProperty("time", NullValueHandling = NullValueHandling.Ignore)]
    public long? Time { get; set; }

    [JsonProperty("crc")]
    public long Crc { get; set; }

    [JsonProperty("dataLocalOffset", NullValueHandling = NullValueHandling.Ignore)]
    public long? DataLocalOffset { get; set; }
}