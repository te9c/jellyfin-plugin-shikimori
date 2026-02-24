using System.Text;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Shikimori.Api
{
    public class ShikimoriApi
    {
        // WARNING: Usage of empty or invalid application name could result in ip ban for shikimori!
        // See: https://shikimori.one/oauth
        public string ApplicationName { get; init; }

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

        public ShikimoriApi(string applicationName)
        {
            ApplicationName = applicationName;
        }

        private async Task<GraphQlResponse?> WebRequestApi(GraphQlRequest request)
        {
            var httpClient = ShikimoriPlugin.Instance!.HttpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", ApplicationName);

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(ApiLink, content).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            return JsonConvert.DeserializeObject<GraphQlResponse>(
                    await response.Content.ReadAsStringAsync().ConfigureAwait(false),
                    new AnimeBaseConverter()
                    );
        }

        public async Task<IEnumerable<AnimeBase>> SearchAnimesAsync(SearchOptions options)
        {
            var request = new GraphQlRequest()
            {
                query = SearchQuery.Replace("{0}", options.ToString())
            };

            var graphQlResponse = await WebRequestApi(request).ConfigureAwait(false);

            return graphQlResponse?.data?.animes == null ? Enumerable.Empty<AnimeBase>() : graphQlResponse.data.animes;
        }

        public async Task<Anime?> GetAnimeAsync(long id, bool censored = true)
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

            var graphQlResponse = await WebRequestApi(request).ConfigureAwait(false);
            var animes = graphQlResponse?.data?.animes;
            if (animes == null || animes.Count() == 0) return null;

            var animeJson = animes.First();

            return (Anime)animeJson;
        }
    }
}
