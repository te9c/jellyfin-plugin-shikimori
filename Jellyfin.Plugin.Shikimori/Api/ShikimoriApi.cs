using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Shikimori.Api
{
    public class ShikimoriApi
    {
        private const int MaxRequestAttempts = 4;
        private static readonly SemaphoreSlim RequestGate = new SemaphoreSlim(1, 1);
        private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(750);
        private static readonly TimeSpan DefaultRateLimitDelay = TimeSpan.FromSeconds(10);
        private static DateTimeOffset _nextRequestAt = DateTimeOffset.MinValue;

        // WARNING: Usage of empty or invalid application name could result in ip ban for shikimori!
        // See: https://shikimori.one/oauth
        public string ApplicationName { get; init; }
        private readonly ILogger _logger;

        private string ApiLink => $"{ShikimoriPlugin.Instance!.ShikimoriBaseUrl}/api/graphql";

        private const string AnimeQuery = @"{
  animes({0}) {
    id

    name
    russian
    japanese
    english

    description
    descriptionHtml
    descriptionSource

    airedOn {
      year
      month
      day
    }
    releasedOn {
      year
      month
      day
    }

    genres {
      id
      name
      russian
    }
    kind

    score

    rating

    studios {
      id
      imageUrl
      name
    }

    poster {
      id
      originalUrl
      mainUrl
      previewUrl
    }

    status
  }
}";
        private const string SearchQuery = @"{
  animes({0}) {
    id

    name
    russian
    japanese
    kind

    airedOn {
      year
      month
      day
    }

    poster {
      id
      previewUrl
    }
  }
}";

        public ShikimoriApi(string applicationName, ILogger logger)
        {
            ApplicationName = applicationName;
            _logger = logger;
        }

        private static TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter?.Delta is { } delta && delta > TimeSpan.Zero)
            {
                return delta;
            }

            if (retryAfter?.Date is { } date)
            {
                var delay = date - DateTimeOffset.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    return delay;
                }
            }

            return TimeSpan.FromSeconds(Math.Min(DefaultRateLimitDelay.TotalSeconds, Math.Pow(2, attempt)));
        }

        private static async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
        {
            var delay = _nextRequestAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<HttpResponseMessage> PostWithThrottleAsync(GraphQlRequest request, CancellationToken cancellationToken)
        {
            await RequestGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

                var httpClient = ShikimoriPlugin.Instance!.HttpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", ApplicationName);

                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(ApiLink, content, cancellationToken).ConfigureAwait(false);

                var nextRequestAt = DateTimeOffset.UtcNow + MinRequestInterval;
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryDelay = GetRetryDelay(response, 1);
                    nextRequestAt = DateTimeOffset.UtcNow + retryDelay;
                }
                _nextRequestAt = nextRequestAt;

                return response;
            }
            finally
            {
                RequestGate.Release();
            }
        }

        private static bool ShouldRetry(HttpResponseMessage response)
        {
            return response.StatusCode == HttpStatusCode.TooManyRequests
                || (int)response.StatusCode >= 500;
        }

        private async Task<GraphQlResponse?> WebRequestApi(GraphQlRequest request, CancellationToken cancellationToken)
        {
            for (var attempt = 1; attempt <= MaxRequestAttempts; attempt++)
            {
                using var response = await PostWithThrottleAsync(request, cancellationToken).ConfigureAwait(false);
                if (ShouldRetry(response))
                {
                    if (attempt == MaxRequestAttempts)
                    {
                        _logger.LogWarning("Shikimori API returned HTTP {StatusCode} after {AttemptCount} attempts. Skipping this metadata request.", (int)response.StatusCode, MaxRequestAttempts);
                        return null;
                    }

                    var delay = GetRetryDelay(response, attempt);
                    _logger.LogWarning("Shikimori API returned HTTP {StatusCode}. Retrying in {DelaySeconds:0.##} seconds ({Attempt}/{MaxAttempts}).", (int)response.StatusCode, delay.TotalSeconds, attempt, MaxRequestAttempts);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Shikimori API returned HTTP {StatusCode}. Skipping this metadata request.", (int)response.StatusCode);
                    return null;
                }

                return JsonConvert.DeserializeObject<GraphQlResponse>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                    new AnimeBaseConverter()
                    );
            }

            return null;
        }

        public async Task<IEnumerable<AnimeBase>> SearchAnimesAsync(SearchOptions options, CancellationToken cancellationToken)
        {
            var request = new GraphQlRequest()
            {
                query = SearchQuery.Replace("{0}", options.ToString())
            };

            var graphQlResponse = await WebRequestApi(request, cancellationToken).ConfigureAwait(false);

            return graphQlResponse?.data?.animes == null ? Enumerable.Empty<AnimeBase>() : graphQlResponse.data.animes;
        }

        public async Task<Anime?> GetAnimeAsync(long id, bool censored, CancellationToken cancellationToken)
        {
            var options = new SearchOptions()
            {
                ids = id.ToString(),
                censored = censored
            };
            var request = new GraphQlRequest()
            {
                query = AnimeQuery.Replace("{0}", options.ToString())
            };

            var graphQlResponse = await WebRequestApi(request, cancellationToken).ConfigureAwait(false);
            var animes = graphQlResponse?.data?.animes;
            if (animes == null || animes.Count() == 0) return null;

            var animeJson = animes.First();

            return (Anime)animeJson;
        }
    }
}
