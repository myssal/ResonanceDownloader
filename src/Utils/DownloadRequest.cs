using Downloader;
using ResonanceTools.Utility;

namespace ResonanceDownloader.Utils
{
    public static class DownloadRequest
    {
        private static readonly DownloadConfiguration _config = new()
        {
            ChunkCount = 8,
            MaxTryAgainOnFailure = 3,
            Timeout = 10000,
            ParallelDownload = true,
            BufferBlockSize = 8192,
            RequestConfiguration = { Timeout = 30000 }
        };

        public static async Task<(bool success, string url, string outputPath)> DownloadFileAsync(string url,
            string outputPath)
        {
            var downloader = new DownloadService(_config);

            try
            {
                await downloader.DownloadFileTaskAsync(url, outputPath);
                Log.Info($"Downloaded file saved to: {outputPath}");
                return (true, "", "");
            }
            catch (Exception ex)
            {
                Log.Info($"Download failed for {url}: {ex.Message}");
                return (false, url, outputPath);
            }
        }
    }
}
