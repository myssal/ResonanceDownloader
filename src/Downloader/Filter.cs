using Newtonsoft.Json;

namespace ResonanceDownloader.Downloader;

public class PathConfig
{
    [JsonProperty("includes")]
    public List<string> Includes { get; init; } = new();

    [JsonProperty("excludes")]
    public List<string> Excludes { get; init; } = new();
}

public class PathPresets
{
    private static readonly Dictionary<string, PathConfig> _configs = new(StringComparer.OrdinalIgnoreCase);

    public void Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Filter file not found", filePath);

        var json = File.ReadAllText(filePath);
        
        var presets = JsonConvert.DeserializeObject<Dictionary<string, PathConfig>>(json);

        if (presets == null || presets.Count == 0)
            throw new InvalidOperationException("No presets defined in filter.json");

        _configs.Clear();
        foreach (var kv in presets)
            _configs[kv.Key] = kv.Value;
    }

    public PathConfig Get(string presetName)
    {
        if (!_configs.TryGetValue(presetName, out var config))
            throw new KeyNotFoundException($"'{presetName}' filter not found in filter.json.");

        return config;
    }

    public IEnumerable<string> AvailablePresets => _configs.Keys;
    
    // "/enemy/",
    // "/enemyplus/",
    // "/environment/env_ui",
    // "/environment/rn_scenes",
    // "/home/",
    // "/item/",
    // "/kabaneri/",
    // "/npc/",
    // "/originpack/",
    // "/role/",
    // "/roleplus/",
    // "/spine/",
    // "/story/",
    // "/texticon/",
    // "/timeline/",
    // "/ui/",
    // "/video/",
    // "/weather/"
    
    // "3d",
    // "model",
    // "commoneffect"
}