using alejandromd.Services;
using Microsoft.Extensions.Caching.Memory;

namespace alejandromd.Endpoints
{
    /// <summary>
    /// Proxies and caches GitHub social preview images. Browsers hotlinking
    /// opengraph.githubassets.com directly get rate limited (HTTP 429) when a
    /// page requests many images at once.
    /// </summary>
    public static class OpenGraphImageEndpoint
    {
        public const string HttpClientName = "OpenGraph";

        private static readonly SemaphoreSlim upstreamLimiter = new(3);
        private static readonly TimeSpan cacheDuration = TimeSpan.FromDays(1);

        public static string GetUrl(string owner, string repo) => $"/og/{owner}/{repo}";

        public static IEndpointRouteBuilder MapOpenGraphImages(this IEndpointRouteBuilder app)
        {
            app.MapGet("/og/{owner}/{repo}", HandleAsync);
            return app;
        }

        private static async Task<IResult> HandleAsync(
            string owner,
            string repo,
            HttpContext context,
            IGitHubService gitHub,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            var key = $"og:{owner}/{repo}".ToLowerInvariant();

            if (!cache.TryGetValue(key, out (byte[] Bytes, string ContentType) image))
            {
                // Only proxy repositories that are shown on the site, so this is not an open proxy
                var known = await gitHub.GetAllRepositoriesAsync();
                if (!known.Any(r => string.Equals(r.Owner, owner, StringComparison.OrdinalIgnoreCase)
                                 && string.Equals(r.Name, repo, StringComparison.OrdinalIgnoreCase)))
                {
                    return Results.NotFound();
                }

                var fetched = await FetchAsync(owner, repo, httpClientFactory, loggerFactory, context.RequestAborted);
                if (fetched is null)
                {
                    return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
                }

                image = fetched.Value;
                cache.Set(key, image, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = cacheDuration,
                    Size = image.Bytes.Length
                });
            }

            context.Response.Headers.CacheControl = $"public, max-age={(int)cacheDuration.TotalSeconds}";
            return Results.Bytes(image.Bytes, image.ContentType);
        }

        private static async Task<(byte[] Bytes, string ContentType)?> FetchAsync(
            string owner,
            string repo,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken)
        {
            var logger = loggerFactory.CreateLogger(typeof(OpenGraphImageEndpoint));
            var client = httpClientFactory.CreateClient(HttpClientName);
            var url = $"https://opengraph.githubassets.com/1/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}";

            await upstreamLimiter.WaitAsync(cancellationToken);
            try
            {
                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    using var response = await client.GetAsync(url, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                        var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/png";
                        return (bytes, contentType);
                    }

                    if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests || attempt == 3)
                    {
                        logger.LogWarning("Failed to fetch social image for {Owner}/{Repo}: {Status}", owner, repo, (int)response.StatusCode);
                        return null;
                    }

                    var delay = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2 * attempt);
                    await Task.Delay(delay > TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : delay, cancellationToken);
                }

                return null;
            }
            finally
            {
                upstreamLimiter.Release();
            }
        }
    }
}
