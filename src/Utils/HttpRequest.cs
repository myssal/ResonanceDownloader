using ResonanceTools.Utility;

namespace ResonanceDownloader.Utils;

public class HttpRequest
{
    private static readonly HttpClient Client = new HttpClient();

    public static bool DownloadFile(string url, string filePath, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Log.Info($"Downloading {url} -> {filePath}");
                using var response = Client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                response.Content.CopyToAsync(fs).Wait();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Attempt {attempt} failed: {ex.Message}");
                if (attempt == maxRetries)
                {
                    Log.Warn("All retry attempts failed.");
                    return false;
                }
            }
        }
        return false;
    }
    
    public static List<string> DownloadFilesParallel(
        List<(string url, string filePath)> downloads, 
        int maxDegreeOfParallelism = 4, 
        int maxRetries = 3)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        var failedDownloads = new List<string>();
        var downloadedCount = 0;
        
        Log.Info($"Start downloading {downloads.Count} files...");
        var tasks = downloads.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                if (!DownloadFile(item.url, item.filePath, maxRetries))
                {
                    failedDownloads.Add(item.url);
                }

                Interlocked.Increment(ref downloadedCount);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        Task.WaitAll(tasks);
        return failedDownloads;
    }
}