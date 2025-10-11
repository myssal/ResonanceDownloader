using System.CommandLine;

namespace ResonanceDownloader;
class Program
{
    static async Task Main(string[] args)
    {
        // Options
        var outputPathOption = new Option<string>(
            "--output", 
            "Specify output folder for download contents.") { IsRequired = false };
        outputPathOption.AddAlias("-o");

        var versionOption = new Option<string>(
            "--game-version",
            "Specify game version. If not set, version will be fetched from server.");
        versionOption.AddAlias("-ver");

        var filterFileOption = new Option<string>(
            "--filter-file",
            "Specify filter JSON file path. If omitted, defaults to filters.json.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        filterFileOption.AddAlias("-f");

        var compareToBaseOption = new Option<bool>(
            "--compare-to-base",
            "Filter updated asset bundles from previous versions. Specify previous version or use option with empty argument to use second most lastest game version patch.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        compareToBaseOption.SetDefaultValue(false);

        var presetNameOption = new Option<string>(
            "--preset", 
            "Specify which preset filter to use from filter file.");

        var downloadCompressedJabOption = new Option<bool>(
            "--download-compressed-jab",
            "Download compressed JAB files.");
        downloadCompressedJabOption.AddAlias("-cjab");
        downloadCompressedJabOption.SetDefaultValue(false);

        var regionOption = new Option<string>(
            "--region", 
            "Specify server region (default: ReleaseB_CN).");
        regionOption.AddAlias("-r");
        regionOption.SetDefaultValue("ReleaseB_CN");

        var platformOption = new Option<string>(
            "--platform",
            "Specify platform (default: StandaloneWindows64).");
        platformOption.AddAlias("-p");
        platformOption.SetDefaultValue("StandaloneWindows64");

        var serverInfoOption = new Option<bool>(
            "--server-info",
            "Show server and platform info list.");

        // Root command
        var rootCommand = new RootCommand("Resonance Solstice Assets Downloader");

        // Add options
        rootCommand.AddOption(outputPathOption);
        rootCommand.AddOption(versionOption);
        rootCommand.AddOption(filterFileOption);
        rootCommand.AddOption(compareToBaseOption);
        rootCommand.AddOption(presetNameOption);
        rootCommand.AddOption(downloadCompressedJabOption);
        rootCommand.AddOption(regionOption);
        rootCommand.AddOption(serverInfoOption);
        rootCommand.AddOption(platformOption);
        
        var binder = new AppOptionsBinder(
            outputPathOption, versionOption, filterFileOption, compareToBaseOption,
            presetNameOption, downloadCompressedJabOption, regionOption, serverInfoOption, platformOption);

        rootCommand.SetHandler((opts) => RunApp(opts), binder);

        await rootCommand.InvokeAsync(args);
    }
    
    static async Task RunApp(AppOptions opts)
    {
        if (opts.ServerInfo)
        {
            ShowServersList();
            return;
        }

        var downloader = new Downloader.Downloader(
            opts.FilterFile,
            opts.DownloadCompressedJab,
            opts.CompareToBase,
            opts.Output,
            opts.Version,
            opts.PresetName,
            opts.Region,
            opts.Platform);
        await downloader.InitializeMetadata();
        await downloader.AssetDownload();
    }
    

    public static void ShowServersList()
    {
        Console.WriteLine("======================================");
        Console.WriteLine("Available Servers");
        Console.WriteLine("======================================");
        Console.WriteLine("CN:");
        Console.WriteLine("- Release: ReleaseB_CN (default)");
        Console.WriteLine("- Debug:   ReleaseB_DBG1/ ReleaseB_DBG2");
        Console.WriteLine("GLB:");
        Console.WriteLine("- Release: ReleaseB_GLB");
        Console.WriteLine("- Debug:   ReleaseB_GLB_DBG");
        Console.WriteLine("JP:");
        Console.WriteLine("- Release: ReleaseB_JP");
        Console.WriteLine("- Debug:   ReleaseB_JP_DBG");
        Console.WriteLine("KR:");
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
}
