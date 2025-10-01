using System.CommandLine;
namespace ResonanceDownloader;
class Program
{
    static async Task Main(string[] args)
    {
        var outputPathArg = new Argument<string>("output", "Specify output folder for download contents.");

        var versionOption = new Option<string>("--game-version",
            "Specify game version. If not set, the version will be fetched from server.");
        versionOption.AddAlias("-gv");

        var filterFileOption = new Option<string>("--filter-file", "Specify filter json file path. If specify with no input, default input will be filters.json.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        filterFileOption.AddAlias("-f");
        
        var presetNameOption = new Option<string>("--preset", "Specify which preset filter to use from filter file. Only valid if --filter-file or -f is specified.");
        presetNameOption.AddAlias("-p");
        
        var downloadCompressedJabOption = new Option<bool>("--download-compressed-jab", "Download compressed jab files instead of extracted asset bundles.");
        downloadCompressedJabOption.AddAlias("-cjab");
        downloadCompressedJabOption.SetDefaultValue(false);

        // Root command
        var rootCommand = new RootCommand("Resonance Solstice Assets Downloader");
        rootCommand.AddArgument(outputPathArg);
        rootCommand.AddOption(versionOption);
        rootCommand.AddOption(filterFileOption);
        rootCommand.AddOption(presetNameOption);
        rootCommand.AddOption(downloadCompressedJabOption);

        rootCommand.SetHandler((output, version, filterFile, presetName, downloadCompressedJab) =>
        {
            var downloader = new Downloader.Downloader(filterFile, downloadCompressedJab, output, version, presetName);
            downloader.AssetDownload();
        }, outputPathArg, versionOption, filterFileOption, presetNameOption, downloadCompressedJabOption);

        await rootCommand.InvokeAsync(args);
    }
}