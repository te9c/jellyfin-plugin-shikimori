using System.Reflection;
using Jellyfin.Plugin.Shikimori.Api;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Shikimori
{
    public enum AnimeType
    {
        Tv,
        Movie
    }

    public class ShikimoriClientManager
    {
        private const string _clientName = "Shikimori Jellyfin plugin";
        private const string _clientId = "4jL1c_MSgZ4qjC8yNotwYGXQmhJ9wFCukQMm_48vGCY";

        public readonly string[] MovieKinds = { "movie" };
        public readonly string[] TvKinds = { "tv", "ona", "ova" };

        private ShikimoriApi _shikimoriApi;
        private ILogger _logger;

        public ShikimoriClientManager(ILogger<ShikimoriClientManager> logger)
        {
            _shikimoriApi = new ShikimoriApi(_clientName, logger);

            _logger = logger;
        }

        public async Task<List<RemoteSearchResult>> SearchAnimesAsync(string name, CancellationToken cancellationToken, AnimeType? type = null, int? year = null)
        {
            _logger.LogDebug($"Searching {name}");
            var searchResult = (await _shikimoriApi.SearchAnimesAsync(new SearchOptions
            {
                search = name,
                limit = ShikimoriPlugin.Instance!.Configuration.SearchLimit,
                kind = type switch
                {
                    AnimeType.Movie => string.Join(',', MovieKinds),
                    AnimeType.Tv => string.Join(',', TvKinds),
                    _ => null
                },
                censored = !ShikimoriPlugin.Instance!.Configuration.ShowCensored
            }, cancellationToken).ConfigureAwait(false)).ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var result = new List<RemoteSearchResult>();
            foreach (var anime in searchResult.Where(i =>
            {
                if (year.HasValue && i.airedOn != null && i.airedOn.year.HasValue)
                {
                    // One year tolerance
                    return Math.Abs(year.Value - i.airedOn.year.Value) <= 1;
                }

                return true;
            }))
            {
                result.Add(anime.ToSearchResult());
            }

            cancellationToken.ThrowIfCancellationRequested();

            return result;
        }

        public async Task<Anime?> GetAnimeAsync(long id, CancellationToken cancellationToken, AnimeType? type = null)
        {
            var anime = await _shikimoriApi.GetAnimeAsync(id, !ShikimoriPlugin.Instance!.Configuration.ShowCensored, cancellationToken).ConfigureAwait(false);
            if (anime == null) return anime;

            cancellationToken.ThrowIfCancellationRequested();

            return type switch
            {
                AnimeType.Tv => TvKinds.Contains(anime.kind) ? anime : null,
                AnimeType.Movie => MovieKinds.Contains(anime.kind) ? anime : null,
                _ => anime,
            };
        }

        public async Task<Anime?> GetAnimeAsync(string name, CancellationToken cancellationToken, AnimeType? type = null, int? year = null)
        {
            var searchResult = await _shikimoriApi.SearchAnimesAsync(new SearchOptions
            {
                search = name,
                limit = year is null ? 1 : 10,
                kind = type switch
                {
                    AnimeType.Movie => string.Join(',', MovieKinds),
                    AnimeType.Tv => string.Join(',', TvKinds),
                    _ => null
                },
                censored = !ShikimoriPlugin.Instance!.Configuration.ShowCensored
            }, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            searchResult = searchResult.Where(i => {
                if (year.HasValue && i.airedOn != null && i.airedOn.year.HasValue)
                {
                    // One year tolerance
                    return Math.Abs(year.Value - i.airedOn.year.Value) <= 1;
                }

                return true;
            });

            if (!searchResult.Any())
            {
                return null;
            }

            var anime = searchResult.First();
            return await GetAnimeAsync(anime.id, cancellationToken, type).ConfigureAwait(false);
        }
    }
}
