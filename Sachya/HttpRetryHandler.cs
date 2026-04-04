using System.Net;

namespace Sachya;

/// <summary>
/// Shared HTTP retry logic with exponential backoff and jitter.
/// </summary>
internal static class HttpRetryHandler
{
    /// <summary>
    /// Executes an HTTP request with retry logic for transient failures (429, 5xx, timeouts).
    /// </summary>
    internal static async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpClient client,
        Func<HttpRequestMessage> requestFactory,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        TimeSpan delay = TimeSpan.FromSeconds(1);
        TimeSpan maxDelay = TimeSpan.FromSeconds(30);

        while (true)
        {
            try
            {
                using var request = requestFactory();
                var response = await client.SendAsync(request, cancellationToken);

                if (((int)response.StatusCode == 429 || (int)response.StatusCode >= 500) && retryCount < maxRetries)
                {
                    retryCount++;
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = NextDelay(delay, maxDelay);
                    continue;
                }

                return response;
            }
            catch (HttpRequestException ex) when (retryCount < maxRetries &&
                (ex.StatusCode == HttpStatusCode.TooManyRequests ||
                 ex.StatusCode >= HttpStatusCode.InternalServerError))
            {
                retryCount++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = NextDelay(delay, maxDelay);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && retryCount < maxRetries)
            {
                retryCount++;
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = NextDelay(delay, maxDelay);
            }
        }
    }

    private static TimeSpan NextDelay(TimeSpan current, TimeSpan max)
    {
        double jitter = 0.8 + Random.Shared.NextDouble() * 0.4;
        return TimeSpan.FromMilliseconds(
            Math.Min(max.TotalMilliseconds, current.TotalMilliseconds * 2) * jitter);
    }
}
