using System.Net.Http;
using ResonanceTools.Utility;

namespace ResonanceDownloader.Utils
{
    public static class DownloadRequest
    {
        private static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// Downloads a single file asynchronously with retry and optional stream consumer.
        /// </summary>
        public static async Task<bool> DownloadFileAsync(
            string url,
            string filePath,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Log.Info($"Downloading {url} -> {filePath}");

                    using var request = new HttpRequestMessage(HttpMethod.Get, url);

                    using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);

                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await stream.CopyToAsync(fs, cancellationToken);

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error($"Attempt {attempt} failed for {url}: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        Log.Warn($"All retry attempts failed for {url}");
                        return false;
                    }

                    await Task.Delay(1000 * attempt, cancellationToken); 
                }
            }
            return false;
        }
        
        public static async Task<List<string>> DownloadFilesParallelAsync(
            IEnumerable<(string url, string filePath)> downloads,
            int maxDegreeOfParallelism = 8,
            int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            var failedDownloads = new List<string>();
            var downloadedCount = 0;

            Log.Info($"Starting parallel download of {downloads.Count()} files...");

            await Parallel.ForEachAsync(downloads, new ParallelOptions
            {
                MaxDegreeOfParallelism = maxDegreeOfParallelism,
                CancellationToken = cancellationToken
            },
            async (item, token) =>
            {
                bool success = await DownloadFileAsync(
                    item.url,
                    item.filePath,
                    maxRetries: maxRetries,
                    cancellationToken: token
                );

                if (!success)
                    lock (failedDownloads)
                        failedDownloads.Add(item.url);

                int count = Interlocked.Increment(ref downloadedCount);
                if (count % 10 == 0)
                    Log.Info($"Progress: {count}/{downloads.Count()} files completed");
            });

            Log.Info($"Download complete. Success: {downloads.Count() - failedDownloads.Count}, Failed: {failedDownloads.Count}");
            return failedDownloads;
        }
    }
}
