namespace Validpack.Utils;

/// <summary>
/// Helper-Klasse für HTTP-Requests
/// </summary>
public static class HttpHelper
{
    private static readonly HttpClient _client;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private const int MinDelayBetweenRequestsMs = 100;
    
    static HttpHelper()
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "Validpack-Scanner/1.0");
        _client.Timeout = TimeSpan.FromSeconds(30);
    }
    
    /// <summary>
    /// Prüft ob eine URL erreichbar ist (HTTP HEAD oder GET)
    /// </summary>
    /// <returns>True wenn Status 200-299, False bei 404, null bei anderen Fehlern</returns>
    public static async Task<bool?> CheckUrlExistsAsync(string url)
    {
        await RateLimitAsync();
        
        try
        {
            // Erst HEAD versuchen (schneller, weniger Traffic)
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _client.SendAsync(request);
            
            // Bei 405 (Method Not Allowed) mit GET versuchen
            if (response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
            {
                request = new HttpRequestMessage(HttpMethod.Get, url);
                response = await _client.SendAsync(request);
            }
            
            if (response.IsSuccessStatusCode)
                return true;
                
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;
                
            // Andere Fehler (z.B. Rate Limiting, Server Error)
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Rate-Limiting: Mindestens MinDelayBetweenRequestsMs zwischen Requests
    /// </summary>
    private static async Task RateLimitAsync()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            var timeSinceLastRequest = DateTime.Now - _lastRequestTime;
            if (timeSinceLastRequest.TotalMilliseconds < MinDelayBetweenRequestsMs)
            {
                var delay = MinDelayBetweenRequestsMs - (int)timeSinceLastRequest.TotalMilliseconds;
                await Task.Delay(delay);
            }
            _lastRequestTime = DateTime.Now;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }
}
