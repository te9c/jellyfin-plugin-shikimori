using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Shikimori.Configuration
{
    public enum TitlePreferenceType
    {
        Romaji,
        Russian,
        Japanese
    }

    public enum GenreTitleLanguagePreferenceType
    {
        English,
        Russian
    }
    public class PluginConfiguration : BasePluginConfiguration
    {
        public PluginConfiguration()
        {
            SearchLimit = 10;
            TitlePreference = TitlePreferenceType.Russian;
            OriginalTitlePreference = TitlePreferenceType.Romaji;
            SearchTitlePreference = TitlePreferenceType.Russian;
            GenreTitleLanguagePreference = GenreTitleLanguagePreferenceType.Russian;
            ShowCensored = false;
            _shikimoriBaseUrl = ShikimoriPlugin.ShikimoriBaseUrlDefault;
        }
        private int _searchLimit;
        public int SearchLimit
        {
            get => _searchLimit;
            // This limit is because of shikimori api restriction
            set => _searchLimit = (value >= 1 && value <= 50) ? value : _searchLimit;
        }

        public TitlePreferenceType TitlePreference { get; set; }
        public TitlePreferenceType OriginalTitlePreference { get; set; }
        public TitlePreferenceType SearchTitlePreference { get; set; }
        public GenreTitleLanguagePreferenceType GenreTitleLanguagePreference { get; set; }
        public bool ShowCensored { get; set; }
        
        // TODO: make some kind of check on correctness of url.
        private String _shikimoriBaseUrl;

        public String ShikimoriBaseUrl
        {
            get => _shikimoriBaseUrl;
            set => _shikimoriBaseUrl = value.TrimEnd('/');
        }
    }
}
