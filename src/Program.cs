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
        filterFileOption.SetDefaultValue("filters.json");
        
        var presetNameOption = new Option<string>("--preset", "Specify which preset filter to use from filter file. Only valid if --filter-file or -f is specified.");
        presetNameOption.AddAlias("-p");

        // Root command
        var rootCommand = new RootCommand("Resonance Solstice Assets Downloader");
        rootCommand.AddArgument(outputPathArg);
        rootCommand.AddOption(versionOption);
        rootCommand.AddOption(filterFileOption);
        rootCommand.AddOption(presetNameOption);

        rootCommand.SetHandler((output, version, filterFile, presetName) =>
        {
            var downloader = new Downloader.Downloader(output, version, filterFile, presetName);
            downloader.AssetDownload();
        }, outputPathArg, versionOption, filterFileOption, presetNameOption);

        await rootCommand.InvokeAsync(args);
    }
}