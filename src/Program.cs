using System.CommandLine;
using ResonanceDownloader.Downloader;
using ResonanceTools.Utility;

namespace ResonanceDownloader;
class Program
{
    static async Task Main(string[] args)
    {
        var outputPathOption = new Option<string>("output", "Specify output folder for download contents.");
        outputPathOption.AddAlias("-o");

        var versionOption = new Option<string>("--game-version",
            "Specify game version. If not set, the version will be fetched from server.");
        versionOption.AddAlias("-gv");
        
        var filterFileOption = new Option<string>("--filter-file", "Specify filter json file path. If specify with no input, default input will be filters.json.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        filterFileOption.AddAlias("-f");
        
        var presetNameOption = new Option<string>("--preset", "Specify which preset filter to use from filter file. Only valid if --filter-file or -f is specified.");
        presetNameOption.AddAlias("-pr");
        
        var downloadCompressedJabOption = new Option<bool>("--download-compressed-jab", "Specify to download compressed jab files or not.");
        downloadCompressedJabOption.AddAlias("-cjab");
        downloadCompressedJabOption.SetDefaultValue(false);

        var regionOption = new Option<string>("--region", "Specify server region and type. Default is CN Release. For full server option, use --server-info or -svi.");
        regionOption.AddAlias("-r");
        regionOption.SetDefaultValue("ReleaseB_CN");
        
        var platformOption = new Option<string>("--platform", "Specify platform. Default is PC (StandaloneWindows64). For full platform option, use --server-info or -svi.");
        platformOption.AddAlias("-p");
        platformOption.SetDefaultValue("StandaloneWindows64");
        
        var serverInfoOption = new Option<bool>("--server-info", "Servers info list.");
        serverInfoOption.AddAlias("-svi");

        // Root command
        var rootCommand = new RootCommand("Resonance Solstice Assets Downloader");
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(versionOption);
        rootCommand.AddOption(filterFileOption);
        rootCommand.AddOption(presetNameOption);
        rootCommand.AddOption(downloadCompressedJabOption);
        rootCommand.AddOption(regionOption);
        rootCommand.AddOption(serverInfoOption);
        rootCommand.AddOption(platformOption);

        rootCommand.SetHandler((output, version, filterFile, presetName, downloadCompressedJab, region, serverInfo, 
                platform) =>
        {
            if (serverInfo)
                ShowServersList();
            else
            {
                var downloader = new Downloader.Downloader(filterFile, downloadCompressedJab, output, version,
                    presetName, region, platform);
                downloader.AssetDownload();
            }
            
        }, outputPathOption, versionOption, filterFileOption, presetNameOption, downloadCompressedJabOption, 
        regionOption, serverInfoOption, platformOption);
        
        await rootCommand.InvokeAsync(args);
    }

    public static void ShowServersList()
    {
        Console.WriteLine("======================================");
        Console.WriteLine("Available Servers");
        Console.WriteLine("======================================");
        Console.WriteLine("CN:");
        Console.WriteLine("- Release: ReleaseB_CN (default)");
        Console.WriteLine("- Debug:   ReleaseB_DBG");
        Console.WriteLine("[ GLB ]");
        Console.WriteLine("- Release: ReleaseB_GLB");
        Console.WriteLine("- Debug:   ReleaseB_GLB_DBG");
        Console.WriteLine("[ JP ]");
        Console.WriteLine("- Release: ReleaseB_JP");
        Console.WriteLine("- Debug:   ReleaseB_JP_DBG");
        Console.WriteLine("[ KR ]");
        Console.WriteLine("- Release: ReleaseB_KR");
        Console.WriteLine("- Debug:   ReleaseB_KR_DBG");

        Console.WriteLine("======================================");
        Console.WriteLine("Available Platforms");
        Console.WriteLine("======================================");
        Console.WriteLine("• PC:       StandaloneWindows64 or PC");
        Console.WriteLine("• Android:  Android");
        Console.WriteLine("• iOS:      IOS");
        Console.WriteLine("======================================");
    }

    public static void DumpIndexRelease()
    {
        string basePath = @"";
        foreach (var region in Enum.GetValues<Downloader.IndexType>())
        {
            Utils.HttpRequest.DownloadFile(IndexUrls.GetIndexUrl(region), Path.Combine(basePath, $"{region.ToString()}.txt"));
        }
    }
}
