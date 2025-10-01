using System.CommandLine;
namespace ResonanceDownloader;
class Program
{
    static async Task Main(string[] args)
    {
        var outputPathArg = new Argument<string>(
            "output"
        );
        outputPathArg.Description = "Specify output folder for download contents.";

        var versionOption = new Option<string>(
            "--game-version",
            "-gv"
        );
        versionOption.Description = "Specify game version. If not set, the version will be fetched from server.";

        // Root command
        var rootCommand = new RootCommand("Resonance Solstice Assets Downloader");
        rootCommand.AddArgument(outputPathArg);
        rootCommand.AddOption(versionOption);

        rootCommand.SetHandler((output, version) =>
        {
            if (!string.IsNullOrEmpty(version))
            {
                var downloader = new Downloader.Downloader(version, output);
                downloader.AssetDownload();
            }
            else
            {
                var downloader = new Downloader.Downloader(output);
                downloader.AssetDownload();
            }
        }, outputPathArg, versionOption);

        await rootCommand.InvokeAsync(args);
    }
}