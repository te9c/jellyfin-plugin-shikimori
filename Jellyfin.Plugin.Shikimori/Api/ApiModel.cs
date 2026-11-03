using System.Text.RegularExpressions;
using Jellyfin.Plugin.Shikimori.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Shikimori.Api
{
    public class Data
    {
        public Anime[]? animes { get; set; }
    }

    public class GraphQlRequest
    {
        public string? query { get; set; }
    }

    public class GraphQlResponse
    {
        public Data? data { get; set; }
    }

    public class SearchOptions
    {
        public string? search { get; set; }
        public int limit { get; set; } = 1;
        public string? kind { get; set; }
        public string? ids { get; set; }
        public bool censored { get; set; } = true;

        public override string ToString()
        {
            List<string> result = new List<string>();
            if (search != null)
            {
                result.Add($"search: \"{search}\"");
            }
            result.Add($"limit: {limit.ToString()}");
            if (kind != null)
            {
                result.Add($"kind: \"{kind}\"");
            }
            if (ids != null)
            {
                result.Add($"ids: \"{ids.ToString()}\"");
            }
            if (censored) {
                result.Add("censored: true");
            } else {
                result.Add("censored: false");
            }

            return string.Join(',', result);
        }
    }

    public class Genre
    {
        public long id { get; set; }
        public string? name { get; set; }
        public string? russian { get; set; }
    }

    public class Studio
    {
        public long id { get; set; }
        public string? imageUrl { get; set; }
        public string? name { get; set; }
    }

    // TODO: Make PosterBase class and move id and previewUrl to it.
    public class Poster
    {
        public long id { get; set; }
        public string? originalUrl { get; set; }
        public string? mainUrl { get; set; }
        public string? previewUrl { get; set; }
    }

    public class IncompleteDate
    {
        public int? year { get; set; }
        public int? month { get; set; }
        public int? day { get; set; }

        public DateTime? ToDateTime()
        {
            if (year != null && month != null && day != null)
                return new DateTime(year.Value, month.Value, day.Value);

            return null;
        }
    }

    public class AnimeBase
    {
        public long id { get; set; }

        public string? name { get; set; }
        public string? japanese { get; set; }
        public string? russian { get; set; }
        public string? kind { get; set; }

        public IncompleteDate? airedOn { get; set; }
        public Poster? poster { get; set; }

        protected string? GetPreferedTitle(TitlePreferenceType type)
        {
            // TODO: Fallback languages?
            return type switch
            {
                TitlePreferenceType.Japanese => japanese,
                TitlePreferenceType.Romaji => name,
                TitlePreferenceType.Russian => russian,
                _ => null
            };
        }

        public RemoteSearchResult ToSearchResult()
        {
            var config = ShikimoriPlugin.Instance!.Configuration;

            var result = new RemoteSearchResult()
            {
                Name = GetPreferedTitle(config.SearchTitlePreference),
                ProductionYear = airedOn?.year,
                PremiereDate = airedOn?.ToDateTime(),
                ImageUrl = poster?.previewUrl,
                SearchProviderName = ShikimoriPlugin.ProviderName,
                ProviderIds = new Dictionary<string, string>() { { ShikimoriPlugin.ProviderId, id.ToString() } },
            };

            return result;
        }
    }

    public class Anime : AnimeBase
    {
        public string? english { get; set; }

        public string? description { get; set; }
        public string? descriptionHtml { get; set; }
        public string? descriptionSource { get; set; }

        public IncompleteDate? releasedOn { get; set; }

        public Genre[]? genres { get; set; }

        public float score { get; set; }

        public string? rating { get; set; }

        public Studio[]? studios { get; set; }

        public string? status { get; set; }

        internal string? GetPreferedGenreTitle(GenreTitleLanguagePreferenceType type, Genre genre)
        {
            return type switch
            {
                GenreTitleLanguagePreferenceType.English => genre.name,
                GenreTitleLanguagePreferenceType.Russian => genre.russian,
                _ => null
            };
        }

        private string? FormatRating(string? shikimoriRating)
        {
            return shikimoriRating switch
            {
                "g" => "G",
                "pg" => "PG",
                "pg_13" => "PG-13",
                "r" => "R",
                "r_plus" => "R+",
                "rx" => "Rx",
                _ => null
            };
        }

        private string DeleteTagsInText(string text)
        {
            text = Regex.Replace(text, @"\[.*?\]", ""); // Replace all occurrences of text in square brackets.
            text = Regex.Replace(text, @" ([.,;: ])", "$1"); // Fix trailing spaces left by previous substitue.
            text = Regex.Replace(text, @"[\[\]]", ""); // There are cases when we have
                                                       // left over square brackets from previous substitute

            return text;
        }

        private void FillBaseItem(BaseItem item)
        {
            PluginConfiguration config = ShikimoriPlugin.Instance!.Configuration;

            item.Name = GetPreferedTitle(config.TitlePreference);
            item.OriginalTitle = GetPreferedTitle(config.OriginalTitlePreference);
            item.Overview = string.IsNullOrEmpty(description) ? null : DeleteTagsInText(description);
            item.ProductionYear = airedOn?.year;
            item.PremiereDate = airedOn?.ToDateTime();
            item.EndDate = releasedOn?.ToDateTime();
            item.Genres = genres?.Select(i => GetPreferedGenreTitle(config.GenreTitleLanguagePreference, i)).ToArray();
            item.CommunityRating = score;
            item.OfficialRating = FormatRating(rating);
            item.Studios = studios?.Select(i => { return i.name; }).ToArray();
            item.ProviderIds = new Dictionary<string, string> { { ShikimoriPlugin.ProviderId, id.ToString() } };
        }

        public Movie ToMovie()
        {
            var result = new Movie();
            FillBaseItem(result);

            return result;
        }

        public Series ToSeries()
        {
            var result = new Series()
            {
                Status = status switch
                {
                    "released" => SeriesStatus.Ended,
                    "ongoing" => SeriesStatus.Continuing,
                    "anons" => SeriesStatus.Unreleased,
                    _ => null,
                },
            };
            FillBaseItem(result);

            return result;
        }
    }
}
