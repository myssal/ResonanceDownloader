using ResonanceDownloader.Downloader;
using ResonanceTools.Utility;

namespace ResonanceDownloader.Utils;

public static class EnumParser
{
    /// <summary>
    /// Convert string value to corresponding Platform enum value
    /// </summary>
    /// <param name="platform">input text to parse</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Platform ParsePlatform(string platform)
    {
        if (string.IsNullOrWhiteSpace(platform))
            throw new ArgumentException("Empty or null platform input:", nameof(platform));
        
        platform = platform.Trim();
        
        if (platform.Equals("PC", StringComparison.OrdinalIgnoreCase))
            return Platform.StandaloneWindows64;
        
        if (Enum.TryParse<Platform>(platform, ignoreCase: true, out var result))
            return result;

        throw new ArgumentException($"Invalid platform string: {platform}");
    }
    
    public static bool ParseVersion(string versionString, out Version version)
    {
        version = new Version(0, 0, 0);

        if (string.IsNullOrWhiteSpace(versionString))
            return false;

        var parts = versionString.Split('.');
        if (parts.Length != 3)
        {
            Log.Warn($"Invalid version format: {versionString}");
            return false;
        }

        if (int.TryParse(parts[0], out int major) &&
            int.TryParse(parts[1], out int minor) &&
            int.TryParse(parts[2], out int patch))
        {
            version = new Version(major, minor, patch);
            return true;
        }

        Log.Warn($"Invalid version format: {versionString}");
        return false;
    }
}