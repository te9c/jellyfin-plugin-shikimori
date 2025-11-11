using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Shikimori.Providers
{
    public class ShikimoriExternalId : IExternalId
    {
        public string ProviderName 
            => ShikimoriPlugin.ProviderName;

        public string Key 
            => ShikimoriPlugin.ProviderId;

        public ExternalIdMediaType? Type
            => null;
        

        public bool Supports(IHasProviderIds item)
            => item is Series or Movie;
    }
}
