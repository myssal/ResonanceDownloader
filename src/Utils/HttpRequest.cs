namespace ResonanceDownloader.Utils;

public class HttpRequest
{
    private static readonly HttpClient client = new HttpClient();

    public static bool DownloadFile(string url, string filePath, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                response.Content.CopyToAsync(fs).Wait();

                Console.WriteLine($"Download succeeded on attempt {attempt}: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
                if (attempt == maxRetries)
                {
                    Console.WriteLine("All retry attempts failed.");
                    return false;
                }
            }
        }
        return false;
    }
}